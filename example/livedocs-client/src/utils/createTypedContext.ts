// src\utils\createTypedContext.ts
import React from 'react';

export function createTypedContext<T>(): readonly [() => T, React.Provider<T | undefined>] {
  const context = React.createContext<T | undefined>(undefined);

  const useTypedContext = (): T => {
    const ctx = React.useContext(context);
    if (ctx === undefined) {
      throw new Error('useTypedContext must be used within its provider!');
    }
    return ctx;
  };

  return [useTypedContext, context.Provider] as const;
}
