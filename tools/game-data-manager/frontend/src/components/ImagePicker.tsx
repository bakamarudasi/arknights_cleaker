import { useState, useEffect } from 'react';
import { Modal } from './Modal';
import { Button } from './Button';

interface ImageInfo {
  name: string;
  path: string;
  size: number;
}

interface ImagePickerProps {
  value?: string;
  onChange: (path: string | undefined) => void;
  category: string;
  label?: string;
}

export function ImagePicker({ value, onChange, category, label = '画像' }: ImagePickerProps) {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [images, setImages] = useState<ImageInfo[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [uploading, setUploading] = useState(false);

  const fetchImages = async () => {
    setIsLoading(true);
    try {
      const res = await fetch(`/api/images/${category}`);
      if (res.ok) {
        setImages(await res.json());
      }
    } catch (e) {
      console.error('Failed to fetch images:', e);
    }
    setIsLoading(false);
  };

  useEffect(() => {
    if (isModalOpen) {
      fetchImages();
    }
  }, [isModalOpen, category]);

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setUploading(true);
    const formData = new FormData();
    formData.append('file', file);

    try {
      const res = await fetch(`/api/images/${category}`, {
        method: 'POST',
        body: formData,
      });
      if (res.ok) {
        const data = await res.json();
        onChange(data.path);
        await fetchImages();
      }
    } catch (e) {
      console.error('Failed to upload:', e);
    }
    setUploading(false);
  };

  const handleSelect = (img: ImageInfo) => {
    onChange(img.path);
    setIsModalOpen(false);
  };

  const handleClear = () => {
    onChange(undefined);
  };

  return (
    <div className="space-y-2">
      <label className="block text-sm font-medium text-gray-300">{label}</label>

      <div className="flex items-center gap-3">
        {/* プレビュー */}
        <div className="w-16 h-16 bg-ark-dark border border-gray-700 rounded-lg overflow-hidden flex items-center justify-center">
          {value ? (
            <img src={value} alt="" className="w-full h-full object-cover" />
          ) : (
            <span className="text-gray-500 text-xs">なし</span>
          )}
        </div>

        {/* ボタン */}
        <div className="flex flex-col gap-1">
          <Button type="button" size="sm" variant="secondary" onClick={() => setIsModalOpen(true)}>
            選択
          </Button>
          {value && (
            <Button type="button" size="sm" variant="ghost" onClick={handleClear}>
              クリア
            </Button>
          )}
        </div>
      </div>

      {/* 画像選択モーダル */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title={`${label}を選択`}
        size="lg"
      >
        <div className="space-y-4">
          {/* アップロード */}
          <div className="flex items-center gap-4 p-4 bg-ark-darker rounded-lg border border-dashed border-gray-600">
            <input
              type="file"
              accept="image/*"
              onChange={handleUpload}
              className="hidden"
              id="image-upload"
              disabled={uploading}
            />
            <label
              htmlFor="image-upload"
              className="px-4 py-2 bg-ark-accent hover:bg-orange-600 rounded-lg cursor-pointer transition-colors"
            >
              {uploading ? 'アップロード中...' : '新規アップロード'}
            </label>
            <span className="text-sm text-gray-400">PNG, JPG, GIF, WebP</span>
          </div>

          {/* 画像一覧 */}
          {isLoading ? (
            <div className="text-center py-8 text-gray-400">読み込み中...</div>
          ) : images.length === 0 ? (
            <div className="text-center py-8 text-gray-400">画像がありません</div>
          ) : (
            <div className="grid grid-cols-6 gap-3 max-h-80 overflow-y-auto p-2">
              {images.map((img) => (
                <button
                  key={img.name}
                  type="button"
                  onClick={() => handleSelect(img)}
                  className={`
                    relative aspect-square rounded-lg overflow-hidden border-2 transition-all
                    ${value === img.path ? 'border-ark-accent' : 'border-transparent hover:border-gray-500'}
                  `}
                >
                  <img
                    src={img.path}
                    alt={img.name}
                    className="w-full h-full object-cover"
                  />
                  {value === img.path && (
                    <div className="absolute inset-0 bg-ark-accent/20 flex items-center justify-center">
                      <span className="text-white text-lg">✓</span>
                    </div>
                  )}
                </button>
              ))}
            </div>
          )}
        </div>
      </Modal>
    </div>
  );
}

// 小さいプレビュー用コンポーネント
export function ImagePreview({ src, size = 'md' }: { src?: string; size?: 'sm' | 'md' | 'lg' }) {
  const sizeClasses = {
    sm: 'w-8 h-8',
    md: 'w-12 h-12',
    lg: 'w-16 h-16',
  };

  return (
    <div className={`${sizeClasses[size]} bg-ark-dark rounded overflow-hidden flex items-center justify-center`}>
      {src ? (
        <img src={src} alt="" className="w-full h-full object-cover" />
      ) : (
        <span className="text-gray-600 text-xs">-</span>
      )}
    </div>
  );
}
