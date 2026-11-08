import { defineConfig } from '@hey-api/openapi-ts'

/**
 * Generates the typed API client from the document `dotnet build` emits for src/api/Forma.Api.
 * Run `npm run generate:api -w forma-app` after changing the API.
 */
export default defineConfig({
  input: '../api/Forma.Api/OpenApi/forma-api.json',
  output: {
    path: 'src/api/generated',
  },
  plugins: [
    {
      name: '@hey-api/client-fetch',
      runtimeConfigPath: './src/api/client-config',
    },
    '@tanstack/vue-query',
  ],
})
