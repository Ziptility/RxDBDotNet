// src\components\NetworkStatus.tsx
import React, { useState, useEffect } from 'react';
import { Box, Typography, Chip } from '@mui/material';
import { Wifi as WifiIcon, WifiOff as WifiOffIcon, Sync as SyncIcon } from '@mui/icons-material';
import { useOnlineStatus } from '@/hooks/useOnlineStatus';
import { getDatabase } from '@/lib/database';
import { LiveDocsReplicationState } from '@/types';
import { combineLatest } from 'rxjs';
import { RxGraphQLReplicationState } from 'rxdb/plugins/replication-graphql';

const NetworkStatus: React.FC = (): JSX.Element => {
  const isOnline = useOnlineStatus();
  const [isSyncing, setIsSyncing] = useState<boolean>(false);
  const [replicationStates, setReplicationStates] = useState<LiveDocsReplicationState | null>(null);

  useEffect(() => {
    const initReplication = async (): Promise<void> => {
      try {
        const db = await getDatabase();
        if (db.replicationStates) {
          setReplicationStates(db.replicationStates);
        } else {
          console.warn('Replication states not available');
        }
      } catch (error) {
        console.error('Error initializing replication:', error);
        // Handle the error appropriately, e.g., show an error message to the user
      }
    };

    void initReplication();
  }, []);

  useEffect(() => {
    if (!replicationStates) return;

    const subscription = combineLatest(
      Object.values(replicationStates).map((state: RxGraphQLReplicationState<unknown, unknown>) => state.active$)
    ).subscribe((activeStates: boolean[]) => {
      setIsSyncing(activeStates.some(Boolean));
    });

    return (): void => {
      subscription.unsubscribe();
    };
  }, [replicationStates]);

  return (
    <Box display="flex" alignItems="center" gap={2}>
      <Typography>{isOnline ? 'Online' : 'Offline'}</Typography>
      {isOnline ? (
        <Chip
          icon={isSyncing ? <SyncIcon /> : <WifiIcon />}
          label={isSyncing ? 'Syncing' : 'Synced'}
          color={isSyncing ? 'warning' : 'success'}
          size="small"
        />
      ) : (
        <Chip icon={<WifiOffIcon />} label="Offline" color="error" size="small" />
      )}
    </Box>
  );
};

export default NetworkStatus;
