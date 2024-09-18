// src\hooks\useDocuments.ts
import { useState, useEffect, useCallback } from 'react';
import { RxCollection, RxDocument, MangoQuery, MangoQuerySelector, MangoQuerySortPart, DeepReadonly } from 'rxdb';
import { getDatabase } from '@/lib/database';
import { LiveDocsDatabase } from '@/types';

interface UseDocumentsResult<T> {
  documents: DeepReadonly<T>[];
  isLoading: boolean;
  error: Error | null;
  refetch: () => Promise<void>;
  upsertDocument: (doc: T) => Promise<void>;
  deleteDocument: (id: string) => Promise<void>;
}

export function useDocuments<T extends { id: string; updatedAt: string; isDeleted?: boolean }>(
  collectionName: keyof LiveDocsDatabase
): UseDocumentsResult<T> {
  const [documents, setDocuments] = useState<DeepReadonly<T>[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<Error | null>(null);
  const [collection, setCollection] = useState<RxCollection<T> | null>(null);

  useEffect(() => {
    const initCollection = async (): Promise<void> => {
      const db = await getDatabase();
      setCollection(db[collectionName] as RxCollection<T>);
    };
    void initCollection();
  }, [collectionName]);

  const fetchDocuments = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    setError(null);

    try {
      if (!collection) {
        throw new Error('Collection is not initialized');
      }

      const query: MangoQuery<T> = {
        selector: {
          isDeleted: { $eq: false },
        } as unknown as MangoQuerySelector<T>,
        sort: [{ updatedAt: 'desc' } as MangoQuerySortPart<T>],
      };

      const docs: RxDocument<T>[] = await collection.find(query).exec();
      setDocuments(docs.map((doc) => doc.toJSON()));
    } catch (err) {
      setError(err instanceof Error ? err : new Error('Failed to fetch documents'));
    } finally {
      setIsLoading(false);
    }
  }, [collection]);

  useEffect(() => {
    void fetchDocuments();
  }, [fetchDocuments]);

  const upsertDocument = useCallback(
    async (doc: T): Promise<void> => {
      if (!collection) return;
      try {
        await collection.upsert(doc);
        await fetchDocuments();
      } catch (err) {
        setError(err instanceof Error ? err : new Error('Failed to upsert document'));
      }
    },
    [collection, fetchDocuments]
  );

  const deleteDocument = useCallback(
    async (id: string): Promise<void> => {
      if (!collection) return;
      try {
        const doc = await collection.findOne(id).exec();
        if (doc) {
          await doc.update({
            $set: {
              isDeleted: true,
              updatedAt: new Date().toISOString(),
            },
          });
        }
        await fetchDocuments();
      } catch (err) {
        setError(err instanceof Error ? err : new Error('Failed to delete document'));
      }
    },
    [collection, fetchDocuments]
  );

  const refetch = useCallback(async (): Promise<void> => {
    await fetchDocuments();
  }, [fetchDocuments]);

  return { documents, isLoading, error, refetch, upsertDocument, deleteDocument };
}
