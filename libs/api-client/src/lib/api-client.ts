import createClient from 'openapi-fetch';
import type { components, paths } from './schema';

export type { components, paths } from './schema';
export type Schemas = components['schemas'];

// Paths already include the /api/v1 prefix, so the default base URL is the same origin
// (in dev the Vite proxy forwards /api to the .NET backend).
export function createApiClient(baseUrl = '') {
  return createClient<paths>({ baseUrl });
}

export const apiClient = createApiClient();
