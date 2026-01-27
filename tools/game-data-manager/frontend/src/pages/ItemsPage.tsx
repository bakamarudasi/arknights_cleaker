import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { DataTable } from '../components/DataTable';
import { Modal, ModalFooter } from '../components/Modal';
import { FormField, Input, Select, TextArea, Checkbox } from '../components/FormField';
import { Button } from '../components/Button';
import { RarityBadge } from '../components/RarityBadge';
import { useDataList, useCreateData, useUpdateData, useDeleteData } from '../hooks/useDataQuery';
import type { ItemData, ItemType, Rarity, ConsumableType } from '../types';

const ITEM_TYPES: { value: ItemType; label: string }[] = [
  { value: 'KeyItem', label: 'キーアイテム' },
  { value: 'Material', label: '素材' },
  { value: 'Consumable', label: '消耗品' },
  { value: 'CostumeUnlock', label: '衣装解放' },
];

const RARITIES: { value: Rarity; label: string }[] = [
  { value: 'Star1', label: '★1' },
  { value: 'Star2', label: '★2' },
  { value: 'Star3', label: '★3' },
  { value: 'Star4', label: '★4' },
  { value: 'Star5', label: '★5' },
  { value: 'Star6', label: '★6' },
];

const CONSUMABLE_TYPES: { value: ConsumableType; label: string }[] = [
  { value: 'None', label: 'なし' },
  { value: 'RecoverSP', label: 'SP回復' },
  { value: 'BoostIncome', label: '収入ブースト' },
  { value: 'InstantMoney', label: '即時金銭' },
  { value: 'RecoverLensBattery', label: 'レンズ充電' },
];

export function ItemsPage() {
  const { data: items = [], isLoading } = useDataList<ItemData>('items');
  const createMutation = useCreateData<ItemData>('items');
  const updateMutation = useUpdateData<ItemData>('items');
  const deleteMutation = useDeleteData('items');

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<ItemData | null>(null);

  const { register, handleSubmit, reset, formState: { errors } } = useForm<ItemData>();

  const openCreateModal = () => {
    setEditingItem(null);
    reset({
      id: '',
      displayName: '',
      description: '',
      type: 'Material',
      rarity: 'Star1',
      sortOrder: 0,
      maxStack: -1,
      sellPrice: 0,
      useEffect: 'None',
      effectValue: 0,
      effectDuration: 0,
      convertAmount: 1,
      targetCostumeIndex: 1,
      effectFormat: '+{0}',
      isPercentDisplay: false,
      categoryIcon: '',
      isSpecial: false,
    });
    setIsModalOpen(true);
  };

  const openEditModal = (item: ItemData) => {
    setEditingItem(item);
    reset(item);
    setIsModalOpen(true);
  };

  const onSubmit = async (data: ItemData) => {
    if (editingItem) {
      await updateMutation.mutateAsync({ id: editingItem.id, item: data });
    } else {
      await createMutation.mutateAsync(data);
    }
    setIsModalOpen(false);
  };

  const columns = [
    { key: 'id', header: 'ID', width: 'w-32' },
    { key: 'displayName', header: '名前' },
    {
      key: 'type',
      header: 'タイプ',
      render: (item: ItemData) => ITEM_TYPES.find(t => t.value === item.type)?.label,
    },
    {
      key: 'rarity',
      header: 'レア度',
      render: (item: ItemData) => <RarityBadge rarity={item.rarity} />,
    },
    { key: 'sortOrder', header: '並び順', width: 'w-20' },
  ];

  if (isLoading) {
    return <div className="text-center py-8">読み込み中...</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold">アイテム管理</h2>
        <Button onClick={openCreateModal}>+ 新規作成</Button>
      </div>

      <DataTable
        data={items}
        columns={columns}
        idField="id"
        onEdit={openEditModal}
        onDelete={(id) => deleteMutation.mutate(id)}
      />

      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title={editingItem ? 'アイテム編集' : 'アイテム新規作成'}
        size="lg"
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <FormField label="ID" required error={errors.id?.message}>
              <Input
                {...register('id', { required: 'IDは必須です' })}
                disabled={!!editingItem}
                error={!!errors.id}
              />
            </FormField>

            <FormField label="表示名" required error={errors.displayName?.message}>
              <Input
                {...register('displayName', { required: '表示名は必須です' })}
                error={!!errors.displayName}
              />
            </FormField>

            <FormField label="タイプ" required>
              <Select {...register('type')} options={ITEM_TYPES} />
            </FormField>

            <FormField label="レア度" required>
              <Select {...register('rarity')} options={RARITIES} />
            </FormField>

            <FormField label="並び順">
              <Input type="number" {...register('sortOrder', { valueAsNumber: true })} />
            </FormField>

            <FormField label="売却価格">
              <Input type="number" {...register('sellPrice', { valueAsNumber: true })} />
            </FormField>
          </div>

          <FormField label="説明">
            <TextArea {...register('description')} rows={3} />
          </FormField>

          <div className="border-t border-gray-700 pt-4">
            <h3 className="text-sm font-medium text-gray-400 mb-3">消耗品設定</h3>
            <div className="grid grid-cols-3 gap-4">
              <FormField label="効果タイプ">
                <Select {...register('useEffect')} options={CONSUMABLE_TYPES} />
              </FormField>
              <FormField label="効果値">
                <Input type="number" step="0.1" {...register('effectValue', { valueAsNumber: true })} />
              </FormField>
              <FormField label="効果時間(秒)">
                <Input type="number" {...register('effectDuration', { valueAsNumber: true })} />
              </FormField>
            </div>
          </div>

          <div className="border-t border-gray-700 pt-4">
            <h3 className="text-sm font-medium text-gray-400 mb-3">ガチャ被り設定</h3>
            <div className="grid grid-cols-2 gap-4">
              <FormField label="変換先アイテムID">
                <Input {...register('convertToItemId')} placeholder="item_id" />
              </FormField>
              <FormField label="変換数">
                <Input type="number" {...register('convertAmount', { valueAsNumber: true })} />
              </FormField>
            </div>
          </div>

          <div className="flex gap-4">
            <Checkbox label="特別アイテム" {...register('isSpecial')} />
            <Checkbox label="パーセント表示" {...register('isPercentDisplay')} />
          </div>

          <ModalFooter>
            <Button type="button" variant="ghost" onClick={() => setIsModalOpen(false)}>
              キャンセル
            </Button>
            <Button type="submit" disabled={createMutation.isPending || updateMutation.isPending}>
              {editingItem ? '更新' : '作成'}
            </Button>
          </ModalFooter>
        </form>
      </Modal>
    </div>
  );
}
