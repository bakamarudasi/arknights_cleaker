import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { DataTable } from '../components/DataTable';
import { Modal, ModalFooter } from '../components/Modal';
import { FormField, Input, Select, TextArea, Checkbox } from '../components/FormField';
import { Button } from '../components/Button';
import { useDataList, useCreateData, useUpdateData, useDeleteData } from '../hooks/useDataQuery';
import type { UpgradeData, UpgradeType, UpgradeCategory, CurrencyType, ItemData } from '../types';

const UPGRADE_TYPES: { value: UpgradeType; label: string }[] = [
  { value: 'Click_FlatAdd', label: 'クリック固定加算' },
  { value: 'Click_PercentAdd', label: 'クリック%加算' },
  { value: 'Income_FlatAdd', label: '自動収入固定加算' },
  { value: 'Income_PercentAdd', label: '自動収入%加算' },
  { value: 'Critical_ChanceAdd', label: 'クリティカル率' },
  { value: 'Critical_PowerAdd', label: 'クリティカル倍率' },
  { value: 'SP_ChargeAdd', label: 'SP回復速度' },
  { value: 'Fever_PowerAdd', label: 'フィーバー倍率' },
];

const CATEGORIES: { value: UpgradeCategory; label: string }[] = [
  { value: 'Click', label: 'クリック' },
  { value: 'Income', label: '自動収入' },
  { value: 'Critical', label: 'クリティカル' },
  { value: 'Skill', label: 'スキル' },
  { value: 'Special', label: '特殊' },
];

const CURRENCIES: { value: CurrencyType; label: string }[] = [
  { value: 'LMD', label: '龍門幣' },
  { value: 'Certificate', label: '資格証' },
  { value: 'Originium', label: '純正源石' },
];

export function UpgradesPage() {
  const { data: upgrades = [], isLoading } = useDataList<UpgradeData>('upgrades');
  const { data: items = [] } = useDataList<ItemData>('items');
  const createMutation = useCreateData<UpgradeData>('upgrades');
  const updateMutation = useUpdateData<UpgradeData>('upgrades');
  const deleteMutation = useDeleteData('upgrades');

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<UpgradeData | null>(null);

  const { register, handleSubmit, reset, formState: { errors } } = useForm<UpgradeData>();

  const itemOptions = [
    { value: '', label: '-- なし --' },
    ...items.map(item => ({ value: item.id, label: `${item.displayName} (${item.id})` })),
  ];

  const upgradeOptions = [
    { value: '', label: '-- なし --' },
    ...upgrades.map(u => ({ value: u.id, label: `${u.displayName} (${u.id})` })),
  ];

  const openCreateModal = () => {
    setEditingItem(null);
    reset({
      id: '',
      displayName: '',
      description: '',
      upgradeType: 'Click_FlatAdd',
      category: 'Click',
      effectValue: 1,
      maxLevel: 10,
      currencyType: 'LMD',
      baseCost: 100,
      costMultiplier: 1.15,
      requiredMaterials: [],
      materialScaling: 1.0,
      prerequisiteLevel: 1,
      scaleWithHolding: false,
      maxHoldingMultiplier: 2.0,
      sortOrder: 0,
      effectFormat: '+{0}',
      isPercentDisplay: false,
      categoryIcon: '',
      isSpecial: false,
    });
    setIsModalOpen(true);
  };

  const openEditModal = (item: UpgradeData) => {
    setEditingItem(item);
    reset(item);
    setIsModalOpen(true);
  };

  const onSubmit = async (data: UpgradeData) => {
    // 空文字のオプショナルフィールドをundefinedに
    if (!data.requiredUnlockItemId) data.requiredUnlockItemId = undefined;
    if (!data.prerequisiteUpgradeId) data.prerequisiteUpgradeId = undefined;
    if (!data.relatedStockId) data.relatedStockId = undefined;

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
      key: 'category',
      header: 'カテゴリ',
      render: (item: UpgradeData) => CATEGORIES.find(c => c.value === item.category)?.label,
    },
    {
      key: 'upgradeType',
      header: 'タイプ',
      render: (item: UpgradeData) => UPGRADE_TYPES.find(t => t.value === item.upgradeType)?.label,
    },
    { key: 'effectValue', header: '効果値' },
    { key: 'maxLevel', header: '最大Lv' },
    {
      key: 'baseCost',
      header: 'コスト',
      render: (item: UpgradeData) => `${item.baseCost.toLocaleString()} ${item.currencyType}`,
    },
  ];

  if (isLoading) {
    return <div className="text-center py-8">読み込み中...</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold">アップグレード管理</h2>
        <Button onClick={openCreateModal}>+ 新規作成</Button>
      </div>

      <DataTable
        data={upgrades}
        columns={columns}
        idField="id"
        onEdit={openEditModal}
        onDelete={(id) => deleteMutation.mutate(id)}
      />

      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title={editingItem ? 'アップグレード編集' : 'アップグレード新規作成'}
        size="xl"
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

            <FormField label="カテゴリ">
              <Select {...register('category')} options={CATEGORIES} />
            </FormField>

            <FormField label="タイプ">
              <Select {...register('upgradeType')} options={UPGRADE_TYPES} />
            </FormField>

            <FormField label="効果値">
              <Input type="number" step="0.1" {...register('effectValue', { valueAsNumber: true })} />
            </FormField>

            <FormField label="最大レベル">
              <Input type="number" {...register('maxLevel', { valueAsNumber: true })} />
            </FormField>
          </div>

          <FormField label="説明">
            <TextArea {...register('description')} rows={2} />
          </FormField>

          <div className="border-t border-gray-700 pt-4">
            <h3 className="text-sm font-medium text-gray-400 mb-3">コスト設定</h3>
            <div className="grid grid-cols-3 gap-4">
              <FormField label="通貨タイプ">
                <Select {...register('currencyType')} options={CURRENCIES} />
              </FormField>
              <FormField label="基本コスト">
                <Input type="number" {...register('baseCost', { valueAsNumber: true })} />
              </FormField>
              <FormField label="コスト倍率">
                <Input type="number" step="0.01" {...register('costMultiplier', { valueAsNumber: true })} />
              </FormField>
            </div>
          </div>

          <div className="border-t border-gray-700 pt-4">
            <h3 className="text-sm font-medium text-gray-400 mb-3">解放条件 (連動)</h3>
            <div className="grid grid-cols-2 gap-4">
              <FormField label="必要アイテム">
                <Select {...register('requiredUnlockItemId')} options={itemOptions} />
              </FormField>
              <FormField label="前提アップグレード">
                <Select {...register('prerequisiteUpgradeId')} options={upgradeOptions} />
              </FormField>
              <FormField label="前提レベル">
                <Input type="number" {...register('prerequisiteLevel', { valueAsNumber: true })} />
              </FormField>
            </div>
          </div>

          <div className="flex gap-4">
            <Checkbox label="特別なアップグレード" {...register('isSpecial')} />
            <Checkbox label="パーセント表示" {...register('isPercentDisplay')} />
            <Checkbox label="株式保有率連動" {...register('scaleWithHolding')} />
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
