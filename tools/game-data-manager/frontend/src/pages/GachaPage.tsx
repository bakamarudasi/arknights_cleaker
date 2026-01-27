import { useState } from 'react';
import { useForm, useFieldArray } from 'react-hook-form';
import { DataTable } from '../components/DataTable';
import { Modal, ModalFooter } from '../components/Modal';
import { FormField, Input, Select, TextArea, Checkbox } from '../components/FormField';
import { Button } from '../components/Button';
import { useDataList, useCreateData, useUpdateData, useDeleteData } from '../hooks/useDataQuery';
import type { GachaBannerData, CurrencyType, ItemData, GachaPoolEntry } from '../types';

const CURRENCIES: { value: CurrencyType; label: string }[] = [
  { value: 'LMD', label: '龍門幣' },
  { value: 'Certificate', label: '資格証' },
  { value: 'Originium', label: '純正源石' },
];

export function GachaPage() {
  const { data: banners = [], isLoading } = useDataList<GachaBannerData>('gacha_banners');
  const { data: items = [] } = useDataList<ItemData>('items');
  const createMutation = useCreateData<GachaBannerData>('gacha_banners');
  const updateMutation = useUpdateData<GachaBannerData>('gacha_banners');
  const deleteMutation = useDeleteData('gacha_banners');

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<GachaBannerData | null>(null);

  const { register, handleSubmit, reset, control, formState: { errors } } = useForm<GachaBannerData>();
  const { fields: poolFields, append: appendPool, remove: removePool } = useFieldArray({
    control,
    name: 'pool',
  });

  const itemOptions = items.map(item => ({ value: item.id, label: `${item.displayName} (${item.id})` }));
  const bannerOptions = [
    { value: '', label: '-- なし --' },
    ...banners.map(b => ({ value: b.bannerId, label: b.bannerName })),
  ];

  const openCreateModal = () => {
    setEditingItem(null);
    reset({
      bannerId: '',
      bannerName: '',
      description: '',
      isLimited: false,
      currencyType: 'Certificate',
      costSingle: 600,
      costTen: 6000,
      hasPity: true,
      pityCount: 50,
      softPityStart: 40,
      pool: [],
      pickupItemIds: [],
      pickupRateBoost: 0.5,
      startsLocked: false,
    });
    setIsModalOpen(true);
  };

  const openEditModal = (item: GachaBannerData) => {
    setEditingItem(item);
    reset(item);
    setIsModalOpen(true);
  };

  const onSubmit = async (data: GachaBannerData) => {
    if (!data.prerequisiteBannerId) data.prerequisiteBannerId = undefined;
    if (!data.requiredUnlockItemId) data.requiredUnlockItemId = undefined;

    if (editingItem) {
      await updateMutation.mutateAsync({ id: editingItem.bannerId, item: data });
    } else {
      await createMutation.mutateAsync(data);
    }
    setIsModalOpen(false);
  };

  const addPoolEntry = () => {
    appendPool({ itemId: '', weight: 1, isPickup: false, stockCount: 0 });
  };

  // 確率計算
  const calculateProbability = (pool: GachaPoolEntry[]) => {
    const totalWeight = pool.reduce((sum, entry) => sum + (entry.weight || 1), 0);
    return pool.map(entry => ({
      ...entry,
      probability: totalWeight > 0 ? ((entry.weight || 1) / totalWeight * 100).toFixed(2) : '0',
    }));
  };

  const columns = [
    { key: 'bannerId', header: 'ID' },
    { key: 'bannerName', header: '名前' },
    {
      key: 'isLimited',
      header: '限定',
      render: (item: GachaBannerData) => item.isLimited ? '限定' : '常設',
    },
    {
      key: 'costSingle',
      header: 'コスト',
      render: (item: GachaBannerData) => `${item.costSingle} ${item.currencyType}`,
    },
    {
      key: 'pool',
      header: '排出数',
      render: (item: GachaBannerData) => `${item.pool.length}種`,
    },
    {
      key: 'hasPity',
      header: '天井',
      render: (item: GachaBannerData) => item.hasPity ? `${item.pityCount}回` : 'なし',
    },
  ];

  if (isLoading) {
    return <div className="text-center py-8">読み込み中...</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold">ガチャバナー管理</h2>
        <Button onClick={openCreateModal}>+ 新規作成</Button>
      </div>

      <DataTable
        data={banners}
        columns={columns}
        idField="bannerId"
        onEdit={openEditModal}
        onDelete={(id) => deleteMutation.mutate(id)}
      />

      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title={editingItem ? 'ガチャ編集' : 'ガチャ新規作成'}
        size="xl"
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <FormField label="バナーID" required error={errors.bannerId?.message}>
              <Input
                {...register('bannerId', { required: 'IDは必須です' })}
                disabled={!!editingItem}
                error={!!errors.bannerId}
              />
            </FormField>

            <FormField label="バナー名" required>
              <Input {...register('bannerName', { required: true })} />
            </FormField>

            <FormField label="通貨タイプ">
              <Select {...register('currencyType')} options={CURRENCIES} />
            </FormField>

            <div className="flex gap-4">
              <Checkbox label="限定バナー" {...register('isLimited')} />
              <Checkbox label="初期ロック" {...register('startsLocked')} />
            </div>
          </div>

          <FormField label="説明">
            <TextArea {...register('description')} rows={2} />
          </FormField>

          <div className="grid grid-cols-2 gap-4">
            <FormField label="単発コスト">
              <Input type="number" {...register('costSingle', { valueAsNumber: true })} />
            </FormField>
            <FormField label="10連コスト">
              <Input type="number" {...register('costTen', { valueAsNumber: true })} />
            </FormField>
          </div>

          <div className="border-t border-gray-700 pt-4">
            <h3 className="text-sm font-medium text-gray-400 mb-3">天井システム</h3>
            <div className="grid grid-cols-3 gap-4">
              <Checkbox label="天井あり" {...register('hasPity')} />
              <FormField label="天井回数">
                <Input type="number" {...register('pityCount', { valueAsNumber: true })} />
              </FormField>
              <FormField label="ソフト天井開始">
                <Input type="number" {...register('softPityStart', { valueAsNumber: true })} />
              </FormField>
            </div>
          </div>

          <div className="border-t border-gray-700 pt-4">
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-medium text-gray-400">排出テーブル</h3>
              <Button type="button" size="sm" variant="secondary" onClick={addPoolEntry}>
                + アイテム追加
              </Button>
            </div>
            <div className="space-y-2 max-h-60 overflow-y-auto">
              {poolFields.map((field, index) => (
                <div key={field.id} className="flex items-center gap-2 bg-ark-darker p-2 rounded">
                  <Select
                    {...register(`pool.${index}.itemId`)}
                    options={[{ value: '', label: '選択...' }, ...itemOptions]}
                    className="flex-1"
                  />
                  <Input
                    type="number"
                    step="0.1"
                    {...register(`pool.${index}.weight`, { valueAsNumber: true })}
                    placeholder="重み"
                    className="w-20"
                  />
                  <Checkbox label="PU" {...register(`pool.${index}.isPickup`)} />
                  <Input
                    type="number"
                    {...register(`pool.${index}.stockCount`, { valueAsNumber: true })}
                    placeholder="在庫"
                    className="w-20"
                  />
                  <Button type="button" size="sm" variant="danger" onClick={() => removePool(index)}>
                    削除
                  </Button>
                </div>
              ))}
            </div>
          </div>

          <div className="border-t border-gray-700 pt-4">
            <h3 className="text-sm font-medium text-gray-400 mb-3">解放条件</h3>
            <div className="grid grid-cols-2 gap-4">
              <FormField label="前提バナー">
                <Select {...register('prerequisiteBannerId')} options={bannerOptions} />
              </FormField>
              <FormField label="必要アイテム">
                <Select
                  {...register('requiredUnlockItemId')}
                  options={[{ value: '', label: '-- なし --' }, ...itemOptions]}
                />
              </FormField>
            </div>
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
