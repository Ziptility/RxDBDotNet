// src\hooks\useDocuments.ts
import { useState, useEffect } from 'react';
import { Subscription } from 'rxjs';
import { getDatabase } from '@/lib/database';
import type { LiveDocsDatabase } from '@/types';
import type { RxCollection, RxDocument, MangoQuerySelector, MangoQuerySortPart } from 'rxdb';

interface Document {
  id: string;
  updatedAt: string;
}

type DocumentWithIsDeleted = Document & { isDeleted?: boolean };

function hasIsDeleted(doc: Document): doc is DocumentWithIsDeleted {
  return 'isDeleted' in doc;
}

function hasUpdatedAt(doc: Document): doc is Document & { updatedAt: string } {
  return 'updatedAt' in doc;
}

interface UseDocumentsResult<T extends Document> {
  documents: T[];
  isLoading: boolean;
  error: Error | null;
  upsertDocument: (doc: T) => Promise<void>;
  deleteDocument: (id: string) => Promise<void>;
}

export function useDocuments<T extends Document>(collectionName: keyof LiveDocsDatabase): UseDocumentsResult<T> {
  const [documents, setDocuments] = useState<T[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    let collection: RxCollection<T> | null = null;
    let subscription: Subscription | null = null;

    const setupSubscription = async (): Promise<void> => {
      try {
        const db = await getDatabase();
        collection = db[collectionName] as RxCollection<T>;
        const selector: MangoQuerySelector<T> = {};
        if (hasIsDeleted({} as T)) {
          (selector as MangoQuerySelector<DocumentWithIsDeleted>).isDeleted = { $ne: true };
        }

        const sort: MangoQuerySortPart<T>[] = [];
        if (hasUpdatedAt({} as T)) {
          sort.push({ updatedAt: 'desc' } as MangoQuerySortPart<T>);
        }

        subscription = collection
          .find({
            selector,
            sort,
          })
          .$.subscribe({
            next: (docs: RxDocument<T>[]) => {
              setDocuments(docs.map((doc) => doc.toJSON() as T));
              setIsLoading(false);
            },
            error: (err: Error) => {
              setError(err);
              setIsLoading(false);
            },
          });
      } catch (err) {
        setError(err instanceof Error ? err : new Error('Failed to setup subscription'));
        setIsLoading(false);
      }
    };

    void setupSubscription();

    return () => {
      if (subscription) {
        subscription.unsubscribe();
      }
    };
  }, [collectionName]);

  const upsertDocument = async (doc: T): Promise<void> => {
    const db = await getDatabase();
    const collection = db[collectionName] as RxCollection<T>;
    await collection.upsert(doc);
  };

  const deleteDocument = async (id: string): Promise<void> => {
    const db = await getDatabase();
    const collection = db[collectionName] as RxCollection<T>;
    const docToUpdate = await collection.findOne(id).exec();
    if (docToUpdate && hasIsDeleted(docToUpdate)) {
      await docToUpdate.update({
        $set: {
          isDeleted: true,
          updatedAt: new Date().toISOString(),
        },
      });
    }
  };

  return { documents, isLoading, error, upsertDocument, deleteDocument };
}
