import axios from 'axios';
import type { DataType, DependencyGraph, ValidationResult } from '../types';

const api = axios.create({
  baseURL: '/api',
});

// ========================================
// Generic CRUD
// ========================================

export async function getAll<T>(dataType: DataType): Promise<T[]> {
  const { data } = await api.get<T[]>(`/data/${dataType}`);
  return data;
}

export async function getById<T>(dataType: DataType, id: string): Promise<T> {
  const { data } = await api.get<T>(`/data/${dataType}/${id}`);
  return data;
}

export async function create<T>(dataType: DataType, item: Partial<T>): Promise<T> {
  const { data } = await api.post<T>(`/data/${dataType}`, item);
  return data;
}

export async function update<T>(dataType: DataType, id: string, item: Partial<T>): Promise<T> {
  const { data } = await api.put<T>(`/data/${dataType}/${id}`, item);
  return data;
}

export async function deleteItem(dataType: DataType, id: string): Promise<void> {
  await api.delete(`/data/${dataType}/${id}`);
}

export async function bulkCreate<T>(dataType: DataType, items: Partial<T>[]): Promise<T[]> {
  const { data } = await api.post<T[]>(`/data/${dataType}/bulk`, items);
  return data;
}

// ========================================
// Validation & Graph
// ========================================

export async function checkReferences(): Promise<ValidationResult> {
  const { data } = await api.get<ValidationResult>('/data/validation/references');
  return data;
}

export async function getDependencyGraph(): Promise<DependencyGraph> {
  const { data } = await api.get<DependencyGraph>('/data/graph/dependencies');
  return data;
}

// ========================================
// Export / Import
// ========================================

export async function exportAll(): Promise<Record<string, unknown[]>> {
  const { data } = await api.get<Record<string, unknown[]>>('/data/export/all');
  return data;
}

export async function importAll(allData: Record<string, unknown[]>): Promise<Record<string, number>> {
  const { data } = await api.post<Record<string, number>>('/data/import/all', allData);
  return data;
}

export default api;
