import type { ContaCorrenteApi } from '../types';
import { httpApi } from './httpApi';
import { mockApi } from './mockApi';

export const usandoMock = import.meta.env.VITE_USE_MOCK === 'true';

export const api: ContaCorrenteApi = usandoMock ? mockApi : httpApi;

export { ApiError } from './client';
