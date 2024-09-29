// src\components\NetworkStatus.tsx
import React, { useEffect, useState } from 'react';
import { Sync as SyncIcon, Wifi as WifiIcon, WifiOff as WifiOffIcon } from '@mui/icons-material';
import { Box, Chip, Tooltip, Typography } from '@mui/material';
import { combineLatest, map } from 'rxjs';
import { useOnlineStatus } from '@/hooks/useOnlineStatus';
import { getDatabase } from '@/lib/database';
import type { Document, LiveDocsReplicationState, LiveDocsReplicationStates } from '@/types';

interface NetworkStatusProps {
  readonly expanded: boolean;
}

const NetworkStatus: React.FC<NetworkStatusProps> = ({ expanded }) => {
  const isOnline = useOnlineStatus();
  const [syncStatus, setSyncStatus] = useState<Record<string, boolean>>({});
  const [replicationStates, setReplicationStates] = useState<LiveDocsReplicationStates | null>(null);

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
      Object.entries(replicationStates).map(([name, state]: [string, LiveDocsReplicationState<Document>]) =>
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
    <Tooltip
      title={
        <Box>
          {Object.entries(syncStatus).map(([name, active]) => (
            <Typography key={name} variant="caption">
              {`${name}: ${active ? 'Syncing' : 'Synced'}`}
            </Typography>
          ))}
        </Box>
      }
    >
      <Chip
        icon={isOnline ? isSyncing ? <SyncIcon /> : <WifiIcon /> : <WifiOffIcon />}
        label={isOnline ? (isSyncing ? 'Syncing' : 'Online') : 'Offline'}
        color={isOnline ? (isSyncing ? 'warning' : 'success') : 'error'}
        size="small"
        sx={{
          width: '100%',
          justifyContent: 'flex-start',
          '& .MuiChip-label': {
            display: expanded ? 'block' : 'none',
            paddingLeft: expanded ? 1 : 0,
            transition: (theme) => theme.transitions.create('padding'),
          },
          '&:hover .MuiChip-label': {
            display: 'block',
          },
        }}
      />
    </Tooltip>
  );
};

export default NetworkStatus;
