import clsx from 'clsx';

interface FormFieldProps {
  label: string;
  required?: boolean;
  error?: string;
  children: React.ReactNode;
  className?: string;
}

export function FormField({ label, required, error, children, className }: FormFieldProps) {
  return (
    <div className={clsx('space-y-1', className)}>
      <label className="block text-sm font-medium text-gray-300">
        {label}
        {required && <span className="text-red-500 ml-1">*</span>}
      </label>
      {children}
      {error && <p className="text-sm text-red-500">{error}</p>}
    </div>
  );
}

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  error?: boolean;
}

export function Input({ error, className, ...props }: InputProps) {
  return (
    <input
      {...props}
      className={clsx(
        'w-full px-3 py-2 bg-ark-dark border rounded-lg focus:outline-none transition-colors',
        error ? 'border-red-500' : 'border-gray-700 focus:border-ark-accent',
        className
      )}
    />
  );
}

interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  options: { value: string; label: string }[];
  error?: boolean;
}

export function Select({ options, error, className, ...props }: SelectProps) {
  return (
    <select
      {...props}
      className={clsx(
        'w-full px-3 py-2 bg-ark-dark border rounded-lg focus:outline-none transition-colors',
        error ? 'border-red-500' : 'border-gray-700 focus:border-ark-accent',
        className
      )}
    >
      {options.map((opt) => (
        <option key={opt.value} value={opt.value}>
          {opt.label}
        </option>
      ))}
    </select>
  );
}

interface TextAreaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  error?: boolean;
}

export function TextArea({ error, className, ...props }: TextAreaProps) {
  return (
    <textarea
      {...props}
      className={clsx(
        'w-full px-3 py-2 bg-ark-dark border rounded-lg focus:outline-none transition-colors resize-none',
        error ? 'border-red-500' : 'border-gray-700 focus:border-ark-accent',
        className
      )}
    />
  );
}

interface CheckboxProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label: string;
}

export function Checkbox({ label, className, ...props }: CheckboxProps) {
  return (
    <label className={clsx('flex items-center gap-2 cursor-pointer', className)}>
      <input
        type="checkbox"
        {...props}
        className="w-4 h-4 rounded border-gray-700 bg-ark-dark text-ark-accent focus:ring-ark-accent"
      />
      <span className="text-sm text-gray-300">{label}</span>
    </label>
  );
}
