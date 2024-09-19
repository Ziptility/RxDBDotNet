// src\utils\createTypedContext.ts
import { createContext, useContext, type Provider } from 'react';

export function createTypedContext<T>(): readonly [() => T, Provider<T | undefined>] {
  const context = createContext<T | undefined>(undefined);

  const useTypedContext = (): T => {
    const ctx = useContext(context);
    if (ctx === undefined) {
      throw new Error('useTypedContext must be used within its provider!');
    }
    return ctx;
  };

  return [useTypedContext, context.Provider] as const;
}
