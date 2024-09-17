// src\hooks\useDocuments.ts
import { useState, useEffect, useCallback } from 'react';
import { RxCollection, RxDocument, MangoQuery, MangoQuerySelector, MangoQuerySortPart, DeepReadonly } from 'rxdb';
import { getDatabase } from '@/lib/database';
import { handleError } from '@/utils/errorHandling';
import { LiveDocsDatabase } from '@/types';

interface UseDocumentsResult<T> {
  documents: DeepReadonly<T>[];
  isLoading: boolean;
  error: Error | null;
  refetch: () => Promise<void>;
}

export function useDocuments<T extends { id: string; updatedAt: string; isDeleted?: boolean }>(
  collectionName: keyof LiveDocsDatabase
): UseDocumentsResult<T> {
  const [documents, setDocuments] = useState<DeepReadonly<T>[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<Error | null>(null);

  const fetchDocuments = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      const db: LiveDocsDatabase = await getDatabase();
      const collection: RxCollection<T> = db[collectionName] as RxCollection<T>;

      const query: MangoQuery<T> = {
        selector: {
          isDeleted: { $eq: false },
        } as unknown as MangoQuerySelector<T>,
        sort: [{ updatedAt: 'desc' } as MangoQuerySortPart<T>],
      };

      const docs: RxDocument<T>[] = await collection.find(query).exec();

      setDocuments(docs.map((doc) => doc.toJSON()));
    } catch (err) {
      const error = err instanceof Error ? err : new Error('An unknown error occurred');
      handleError(error, `Fetching ${collectionName}`);
      setError(error);
    } finally {
      setIsLoading(false);
    }
  }, [collectionName]);

  useEffect(() => {
    void fetchDocuments();
  }, [fetchDocuments]);

  const refetch = useCallback(async (): Promise<void> => {
    await fetchDocuments();
  }, [fetchDocuments]);

  return { documents, isLoading, error, refetch };
}
