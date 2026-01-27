import { useState } from 'react';
import { useForm, useFieldArray } from 'react-hook-form';
import { DataTable } from '../components/DataTable';
import { Modal, ModalFooter } from '../components/Modal';
import { FormField, Input, Select, TextArea, Checkbox } from '../components/FormField';
import { Button } from '../components/Button';
import { useDataList, useCreateData, useUpdateData, useDeleteData } from '../hooks/useDataQuery';
import type { GameEventData, EventTriggerType, MenuType, ItemData } from '../types';

const TRIGGER_TYPES: { value: EventTriggerType; label: string }[] = [
  { value: 'None', label: 'なし' },
  { value: 'MoneyReached', label: '所持金到達' },
  { value: 'ClickCount', label: 'クリック数' },
  { value: 'UpgradePurchased', label: 'アップグレード購入' },
  { value: 'ItemObtained', label: 'アイテム取得' },
  { value: 'TimeElapsed', label: '経過時間' },
  { value: 'AffectionLevel', label: '好感度レベル' },
  { value: 'StockOwned', label: '株式所有' },
];

const MENU_TYPES: { value: MenuType | ''; label: string }[] = [
  { value: '', label: '-- なし --' },
  { value: 'Shop', label: 'ショップ' },
  { value: 'Inventory', label: 'インベントリ' },
  { value: 'Gacha', label: 'ガチャ' },
  { value: 'Market', label: 'マーケット' },
  { value: 'Settings', label: '設定' },
  { value: 'Operator', label: 'オペレーター' },
];

export function EventsPage() {
  const { data: events = [], isLoading } = useDataList<GameEventData>('game_events');
  const { data: items = [] } = useDataList<ItemData>('items');
  const createMutation = useCreateData<GameEventData>('game_events');
  const updateMutation = useUpdateData<GameEventData>('game_events');
  const deleteMutation = useDeleteData('game_events');

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<GameEventData | null>(null);

  const { register, handleSubmit, reset, control, formState: { errors } } = useForm<GameEventData>();
  const { fields: rewardFields, append: appendReward, remove: removeReward } = useFieldArray({
    control,
    name: 'rewardItems',
  });

  const itemOptions = items.map(item => ({ value: item.id, label: `${item.displayName} (${item.id})` }));
  const eventOptions = [
    { value: '', label: '-- なし --' },
    ...events.map(e => ({ value: e.eventId, label: e.eventName })),
  ];

  const openCreateModal = () => {
    setEditingItem(null);
    reset({
      eventId: '',
      eventName: '',
      description: '',
      triggerType: 'None',
      triggerValue: 0,
      oneTimeOnly: true,
      pauseGame: false,
      priority: 0,
      notificationText: '',
      rewardMoney: 0,
      rewardCertificates: 0,
      rewardItems: [],
    });
    setIsModalOpen(true);
  };

  const openEditModal = (item: GameEventData) => {
    setEditingItem(item);
    reset(item);
    setIsModalOpen(true);
  };

  const onSubmit = async (data: GameEventData) => {
    if (!data.requireId) data.requireId = undefined;
    if (!data.prerequisiteEventId) data.prerequisiteEventId = undefined;
    if (!data.unlockMenu) data.unlockMenu = undefined;

    if (editingItem) {
      await updateMutation.mutateAsync({ id: editingItem.eventId, item: data });
    } else {
      await createMutation.mutateAsync(data);
    }
    setIsModalOpen(false);
  };

  const columns = [
    { key: 'eventId', header: 'ID' },
    { key: 'eventName', header: 'イベント名' },
    {
      key: 'triggerType',
      header: 'トリガー',
      render: (item: GameEventData) => TRIGGER_TYPES.find(t => t.value === item.triggerType)?.label,
    },
    {
      key: 'triggerValue',
      header: '条件値',
      render: (item: GameEventData) => item.triggerValue.toLocaleString(),
    },
    {
      key: 'oneTimeOnly',
      header: '一度きり',
      render: (item: GameEventData) => item.oneTimeOnly ? 'Yes' : 'No',
    },
    {
      key: 'rewardMoney',
      header: '報酬金',
      render: (item: GameEventData) => item.rewardMoney > 0 ? item.rewardMoney.toLocaleString() : '-',
    },
  ];

  if (isLoading) {
    return <div className="text-center py-8">読み込み中...</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold">ゲームイベント管理</h2>
        <Button onClick={openCreateModal}>+ 新規作成</Button>
      </div>

      <DataTable
        data={events}
        columns={columns}
        idField="eventId"
        onEdit={openEditModal}
        onDelete={(id) => deleteMutation.mutate(id)}
      />

      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title={editingItem ? 'イベント編集' : 'イベント新規作成'}
        size="xl"
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <FormField label="イベントID" required error={errors.eventId?.message}>
              <Input
                {...register('eventId', { required: 'IDは必須です' })}
                disabled={!!editingItem}
                error={!!errors.eventId}
              />
            </FormField>

            <FormField label="イベント名" required>
              <Input {...register('eventName', { required: true })} />
            </FormField>
          </div>

          <FormField label="説明">
            <TextArea {...register('description')} rows={2} />
          </FormField>

          <div className="border-t border-gray-700 pt-4">
            <h3 className="text-sm font-medium text-gray-400 mb-3">発動条件</h3>
            <div className="grid grid-cols-3 gap-4">
              <FormField label="トリガータイプ">
                <Select {...register('triggerType')} options={TRIGGER_TYPES} />
              </FormField>
              <FormField label="条件値">
                <Input type="number" {...register('triggerValue', { valueAsNumber: true })} />
              </FormField>
              <FormField label="必要ID (オプション)">
                <Input {...register('requireId')} placeholder="upgrade_id など" />
              </FormField>
            </div>
            <div className="grid grid-cols-2 gap-4 mt-4">
              <FormField label="前提イベント">
                <Select {...register('prerequisiteEventId')} options={eventOptions} />
              </FormField>
              <FormField label="優先度">
                <Input type="number" {...register('priority', { valueAsNumber: true })} />
              </FormField>
            </div>
          </div>

          <div className="border-t border-gray-700 pt-4">
            <h3 className="text-sm font-medium text-gray-400 mb-3">報酬設定</h3>
            <div className="grid grid-cols-3 gap-4">
              <FormField label="報酬金 (LMD)">
                <Input type="number" {...register('rewardMoney', { valueAsNumber: true })} />
              </FormField>
              <FormField label="資格証">
                <Input type="number" {...register('rewardCertificates', { valueAsNumber: true })} />
              </FormField>
              <FormField label="解放メニュー">
                <Select {...register('unlockMenu')} options={MENU_TYPES} />
              </FormField>
            </div>

            <div className="mt-4">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm text-gray-400">報酬アイテム</span>
                <Button
                  type="button"
                  size="sm"
                  variant="secondary"
                  onClick={() => appendReward({ itemId: '', amount: 1 })}
                >
                  + 追加
                </Button>
              </div>
              <div className="space-y-2">
                {rewardFields.map((field, index) => (
                  <div key={field.id} className="flex items-center gap-2 bg-ark-darker p-2 rounded">
                    <Select
                      {...register(`rewardItems.${index}.itemId`)}
                      options={[{ value: '', label: '選択...' }, ...itemOptions]}
                      className="flex-1"
                    />
                    <Input
                      type="number"
                      {...register(`rewardItems.${index}.amount`, { valueAsNumber: true })}
                      placeholder="個数"
                      className="w-20"
                    />
                    <Button type="button" size="sm" variant="danger" onClick={() => removeReward(index)}>
                      削除
                    </Button>
                  </div>
                ))}
              </div>
            </div>
          </div>

          <div className="border-t border-gray-700 pt-4">
            <h3 className="text-sm font-medium text-gray-400 mb-3">表示設定</h3>
            <FormField label="通知テキスト">
              <Input {...register('notificationText')} />
            </FormField>
          </div>

          <div className="flex gap-4">
            <Checkbox label="一度きり" {...register('oneTimeOnly')} />
            <Checkbox label="ゲーム一時停止" {...register('pauseGame')} />
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
