//! The Forma plugin ABI (docs/design/plugins.md § 5), small enough to copy into any plugin.
//!
//! The host writes the event (JSON) into memory it got from `forma_alloc`, calls `forma_handle`, and reads the
//! output (JSON) at the pointer and length packed into the returned i64. The instance is thrown away after one
//! call, so nothing here needs to be freed.

#[link(wasm_import_module = "forma")]
extern "C" {
    #[link_name = "log"]
    fn host_log(level: i32, ptr: *const u8, len: i32);
}

#[allow(dead_code)]
pub enum Level {
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
}

/// Writes a line to the plugin's run history (GET /plugins/{id}/runs) and the API log.
pub fn log(level: Level, message: &str) {
    unsafe { host_log(level as i32, message.as_ptr(), message.len() as i32) }
}

#[no_mangle]
pub extern "C" fn forma_alloc(len: i32) -> *mut u8 {
    let mut buffer = Vec::<u8>::with_capacity(len.max(0) as usize);
    let pointer = buffer.as_mut_ptr();
    std::mem::forget(buffer);
    pointer
}

#[no_mangle]
pub extern "C" fn forma_handle(pointer: *mut u8, len: i32) -> i64 {
    let input = unsafe { std::slice::from_raw_parts(pointer, len.max(0) as usize) };
    match crate::handle(input) {
        Some(output) => {
            let output = output.into_boxed_slice();
            let len = output.len() as u64;
            let pointer = Box::into_raw(output) as *mut u8 as u32 as u64;
            ((pointer << 32) | len) as i64
        }
        None => 0,
    }
}
