// src\components\NetworkStatus.tsx
import React, { useState, useEffect } from 'react';
import { Box, Typography, Chip, Tooltip } from '@mui/material';
import { Wifi as WifiIcon, WifiOff as WifiOffIcon, Sync as SyncIcon } from '@mui/icons-material';
import { useOnlineStatus } from '@/hooks/useOnlineStatus';
import { getDatabase } from '@/lib/database';
import { LiveDocsReplicationState, ReplicationCheckpoint } from '@/types';
import { combineLatest, map } from 'rxjs';
import { RxGraphQLReplicationState } from 'rxdb/plugins/replication-graphql';

const NetworkStatus: React.FC = () => {
  const isOnline = useOnlineStatus();
  const [syncStatus, setSyncStatus] = useState<Record<string, boolean>>({});
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
      }
    };

    void initReplication();
  }, []);

  useEffect(() => {
    if (!replicationStates) return;

    const subscription = combineLatest(
      Object.entries(replicationStates).map(
        ([name, state]: [string, RxGraphQLReplicationState<unknown, ReplicationCheckpoint>]) =>
          state.active$.pipe(map((active) => ({ [name]: active })))
      )
    ).subscribe((activeStates: Record<string, boolean>[]) => {
      const mergedStatus = activeStates.reduce((acc, curr) => ({ ...acc, ...curr }), {});
      setSyncStatus(mergedStatus);
    });

    return (): void => {
      subscription.unsubscribe();
    };
  }, [replicationStates]);

  const isSyncing = Object.values(syncStatus).some(Boolean);

  return (
    <Box display="flex" alignItems="center" gap={2}>
      <Typography>{isOnline ? 'Online' : 'Offline'}</Typography>
      {isOnline ? (
        <Tooltip
          title={
            <Box>
              {Object.entries(syncStatus).map(([name, active]) => (
                <Typography key={name}>{`${name}: ${active ? 'Syncing' : 'Synced'}`}</Typography>
              ))}
            </Box>
          }
        >
          <Chip
            icon={isSyncing ? <SyncIcon /> : <WifiIcon />}
            label={isSyncing ? 'Syncing' : 'Synced'}
            color={isSyncing ? 'warning' : 'success'}
            size="small"
          />
        </Tooltip>
      ) : (
        <Chip icon={<WifiOffIcon />} label="Offline" color="error" size="small" />
      )}
    </Box>
  );
};

export default NetworkStatus;
