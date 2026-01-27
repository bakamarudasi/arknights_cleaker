import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { DataTable } from '../components/DataTable';
import { Modal, ModalFooter } from '../components/Modal';
import { FormField, Input, Select, TextArea, Checkbox } from '../components/FormField';
import { Button } from '../components/Button';
import { useDataList, useCreateData, useUpdateData, useDeleteData } from '../hooks/useDataQuery';
import type { CompanyData, CompanyTrait, StockSector, ItemData } from '../types';

const TRAITS: { value: CompanyTrait; label: string }[] = [
  { value: 'None', label: 'なし' },
  { value: 'TechInnovation', label: '技術革新' },
  { value: 'Logistics', label: '物流強化' },
  { value: 'Military', label: '軍事' },
  { value: 'Trading', label: '貿易特化' },
  { value: 'Arts', label: 'アーツ' },
];

const SECTORS: { value: StockSector; label: string }[] = [
  { value: 'Tech', label: 'テクノロジー' },
  { value: 'Military', label: '軍事' },
  { value: 'Logistics', label: '物流' },
  { value: 'Finance', label: '金融' },
  { value: 'Entertainment', label: 'エンタメ' },
  { value: 'Resource', label: '資源' },
];

export function CompaniesPage() {
  const { data: companies = [], isLoading } = useDataList<CompanyData>('companies');
  const { data: items = [] } = useDataList<ItemData>('items');
  const createMutation = useCreateData<CompanyData>('companies');
  const updateMutation = useUpdateData<CompanyData>('companies');
  const deleteMutation = useDeleteData('companies');

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<CompanyData | null>(null);

  const { register, handleSubmit, reset, formState: { errors } } = useForm<CompanyData>();

  const itemOptions = [
    { value: '', label: '-- なし --' },
    ...items.map(item => ({ value: item.id, label: `${item.displayName} (${item.id})` })),
  ];

  const openCreateModal = () => {
    setEditingItem(null);
    reset({
      id: '',
      displayName: '',
      description: '',
      chartColor: '#00FF00',
      themeColor: '#FFFFFF',
      sortOrder: 0,
      traitType: 'None',
      traitMultiplier: 1.0,
      initialPrice: 1000,
      minPrice: 10,
      maxPrice: 0,
      volatility: 0.1,
      drift: 0.02,
      jumpProbability: 0.01,
      jumpIntensity: 0.2,
      transactionFee: 0.01,
      sector: 'Tech',
      totalShares: 1000000,
      dividendRate: 0,
      dividendIntervalSeconds: 0,
      isPlayerCompany: false,
      canSell: true,
    });
    setIsModalOpen(true);
  };

  const openEditModal = (item: CompanyData) => {
    setEditingItem(item);
    reset(item);
    setIsModalOpen(true);
  };

  const onSubmit = async (data: CompanyData) => {
    if (!data.unlockKeyItemId) data.unlockKeyItemId = undefined;

    if (editingItem) {
      await updateMutation.mutateAsync({ id: editingItem.id, item: data });
    } else {
      await createMutation.mutateAsync(data);
    }
    setIsModalOpen(false);
  };

  const columns = [
    { key: 'id', header: 'ID' },
    { key: 'displayName', header: '企業名' },
    {
      key: 'sector',
      header: 'セクター',
      render: (item: CompanyData) => SECTORS.find(s => s.value === item.sector)?.label,
    },
    {
      key: 'traitType',
      header: '特性',
      render: (item: CompanyData) => TRAITS.find(t => t.value === item.traitType)?.label,
    },
    {
      key: 'initialPrice',
      header: '初期株価',
      render: (item: CompanyData) => `${item.initialPrice.toLocaleString()} LMD`,
    },
    {
      key: 'volatility',
      header: 'ボラ',
      render: (item: CompanyData) => `${(item.volatility * 100).toFixed(0)}%`,
    },
    {
      key: 'isPlayerCompany',
      header: '自社株',
      render: (item: CompanyData) => item.isPlayerCompany ? '自社' : '-',
    },
  ];

  if (isLoading) {
    return <div className="text-center py-8">読み込み中...</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold">企業/株式管理</h2>
        <Button onClick={openCreateModal}>+ 新規作成</Button>
      </div>

      <DataTable
        data={companies}
        columns={columns}
        idField="id"
        onEdit={openEditModal}
        onDelete={(id) => deleteMutation.mutate(id)}
      />

      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title={editingItem ? '企業編集' : '企業新規作成'}
        size="xl"
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <FormField label="企業ID" required error={errors.id?.message}>
              <Input
                {...register('id', { required: 'IDは必須です' })}
                disabled={!!editingItem}
                error={!!errors.id}
              />
            </FormField>

            <FormField label="企業名" required>
              <Input {...register('displayName', { required: true })} />
            </FormField>

            <FormField label="セクター">
              <Select {...register('sector')} options={SECTORS} />
            </FormField>

            <FormField label="特性">
              <Select {...register('traitType')} options={TRAITS} />
            </FormField>
          </div>

          <FormField label="説明">
            <TextArea {...register('description')} rows={2} />
          </FormField>

          <div className="border-t border-gray-700 pt-4">
            <h3 className="text-sm font-medium text-gray-400 mb-3">株価設定</h3>
            <div className="grid grid-cols-3 gap-4">
              <FormField label="初期株価">
                <Input type="number" {...register('initialPrice', { valueAsNumber: true })} />
              </FormField>
              <FormField label="最低株価">
                <Input type="number" {...register('minPrice', { valueAsNumber: true })} />
              </FormField>
              <FormField label="最高株価 (0=無制限)">
                <Input type="number" {...register('maxPrice', { valueAsNumber: true })} />
              </FormField>
            </div>
          </div>

          <div className="border-t border-gray-700 pt-4">
            <h3 className="text-sm font-medium text-gray-400 mb-3">変動特性</h3>
            <div className="grid grid-cols-4 gap-4">
              <FormField label="ボラティリティ">
                <Input type="number" step="0.01" {...register('volatility', { valueAsNumber: true })} />
              </FormField>
              <FormField label="ドリフト">
                <Input type="number" step="0.01" {...register('drift', { valueAsNumber: true })} />
              </FormField>
              <FormField label="ジャンプ確率">
                <Input type="number" step="0.01" {...register('jumpProbability', { valueAsNumber: true })} />
              </FormField>
              <FormField label="ジャンプ強度">
                <Input type="number" step="0.01" {...register('jumpIntensity', { valueAsNumber: true })} />
              </FormField>
            </div>
          </div>

          <div className="border-t border-gray-700 pt-4">
            <h3 className="text-sm font-medium text-gray-400 mb-3">取引・配当設定</h3>
            <div className="grid grid-cols-4 gap-4">
              <FormField label="取引手数料">
                <Input type="number" step="0.001" {...register('transactionFee', { valueAsNumber: true })} />
              </FormField>
              <FormField label="発行株式数">
                <Input type="number" {...register('totalShares', { valueAsNumber: true })} />
              </FormField>
              <FormField label="配当率">
                <Input type="number" step="0.01" {...register('dividendRate', { valueAsNumber: true })} />
              </FormField>
              <FormField label="配当間隔(秒)">
                <Input type="number" {...register('dividendIntervalSeconds', { valueAsNumber: true })} />
              </FormField>
            </div>
          </div>

          <div className="border-t border-gray-700 pt-4">
            <h3 className="text-sm font-medium text-gray-400 mb-3">解放条件</h3>
            <FormField label="必要アイテム">
              <Select {...register('unlockKeyItemId')} options={itemOptions} />
            </FormField>
          </div>

          <div className="flex gap-4">
            <Checkbox label="自社株（クリック連動型）" {...register('isPlayerCompany')} />
            <Checkbox label="売却可能" {...register('canSell')} />
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
