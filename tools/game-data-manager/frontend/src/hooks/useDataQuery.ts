import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as api from '../utils/api';
import type { DataType } from '../types';

export function useDataList<T>(dataType: DataType) {
  return useQuery({
    queryKey: [dataType],
    queryFn: () => api.getAll<T>(dataType),
  });
}

export function useDataItem<T>(dataType: DataType, id: string) {
  return useQuery({
    queryKey: [dataType, id],
    queryFn: () => api.getById<T>(dataType, id),
    enabled: !!id,
  });
}

export function useCreateData<T>(dataType: DataType) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (item: Partial<T>) => api.create<T>(dataType, item),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [dataType] });
    },
  });
}

export function useUpdateData<T>(dataType: DataType) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, item }: { id: string; item: Partial<T> }) =>
      api.update<T>(dataType, id, item),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [dataType] });
    },
  });
}

export function useDeleteData(dataType: DataType) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.deleteItem(dataType, id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [dataType] });
    },
  });
}

export function useValidation() {
  return useQuery({
    queryKey: ['validation'],
    queryFn: api.checkReferences,
  });
}

export function useDependencyGraph() {
  return useQuery({
    queryKey: ['graph'],
    queryFn: api.getDependencyGraph,
  });
}
