//! Secure links: an example Forma plugin.
//!
//! - `page.check` (hook): warns about links that use plain `http://`, in buttons, menus and rich text.
//! - `page.published` (notification): logs which page went live.
//!
//! Build: `cargo build --release --target wasm32-unknown-unknown`, then copy
//! `target/wasm32-unknown-unknown/release/secure_links.wasm` to `{Plugins:Path}/secure-links/plugin.wasm`
//! next to `plugin.json`. See README.md.

mod forma;

use forma::{log, Level};
use serde_json::{json, Map, Value};

/// Called once per event with the event's JSON; returns the output JSON, or None for no output.
fn handle(input: &[u8]) -> Option<Vec<u8>> {
    let event: Value = match serde_json::from_slice(input) {
        Ok(event) => event,
        Err(error) => {
            log(Level::Error, &format!("Couldn't read the event: {error}"));
            return None;
        }
    };

    let data = &event["data"];
    match event["event"].as_str() {
        Some("page.check") => {
            let issues = check(&data["document"]);
            log(Level::Debug, &format!("Found {} insecure link(s) on /{}", issues.len(), slug(data)));
            Some(json!({ "issues": issues }).to_string().into_bytes())
        }
        Some("page.published") => {
            log(Level::Info, &format!("“{}” (/{}) is live at revision {}", data["page"]["title"].as_str().unwrap_or("?"), slug(data), data["revision"]));
            None
        }
        _ => None,
    }
}

fn slug(data: &Value) -> &str {
    data["page"]["slug"].as_str().unwrap_or("")
}

/// One issue per `http://` link, pointing at its block, prop and list item like the built-in checks do.
fn check(document: &Value) -> Vec<Value> {
    let mut issues = Vec::new();
    for section in document["sections"].as_array().into_iter().flatten() {
        for block in section["blocks"].as_array().into_iter().flatten() {
            let Some(props) = block["props"].as_object() else { continue };
            for (field, value) in props {
                match value {
                    Value::String(text) => check_value(&mut issues, block, field, None, field, text),
                    // List props (menu links, gallery items): look one level into each item.
                    Value::Array(items) => {
                        for (index, item) in items.iter().enumerate() {
                            for (key, text) in item.as_object().into_iter().flat_map(Map::iter) {
                                if let Value::String(text) = text {
                                    check_value(&mut issues, block, field, Some(index), key, text);
                                }
                            }
                        }
                    }
                    _ => {}
                }
            }
        }
    }
    issues
}

fn check_value(issues: &mut Vec<Value>, block: &Value, field: &str, item: Option<usize>, key: &str, text: &str) {
    let insecure = if key == "html" {
        text.contains("href=\"http://")
    } else {
        key.to_ascii_lowercase().ends_with("href") && text.trim_start().starts_with("http://")
    };
    if insecure {
        issues.push(json!({
            "severity": "warning",
            "code": "link.insecure",
            "message": "This link uses http://. Visitors' browsers may warn about it; use https:// if the site supports it.",
            "blockId": block["id"],
            "field": field,
            "itemIndex": item,
        }));
    }
}
