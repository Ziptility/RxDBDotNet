// src\components\NetworkStatus.tsx
import React, { useEffect, useState } from 'react';
import { Sync as SyncIcon, WifiOff as WifiOffIcon, CheckCircle as CheckCircleIcon } from '@mui/icons-material';
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

  const getStatusIcon = (): React.ReactNode => {
    if (!isOnline) return <WifiOffIcon />;
    if (isSyncing) return <SyncIcon />;
    return <CheckCircleIcon />;
  };

  const getStatusLabel = (): string => {
    if (!isOnline) return 'Offline';
    if (isSyncing) return 'Syncing';
    return 'Synced';
  };

  const getStatusColor = (): 'error' | 'warning' | 'success' => {
    if (!isOnline) return 'error';
    if (isSyncing) return 'warning';
    return 'success';
  };

  return (
    <Tooltip
      title={
        <Box>
          <Typography variant="body2">{isOnline ? 'Connected to server' : 'Working offline'}</Typography>
          {Object.entries(syncStatus).map(([name, active]) => (
            <Typography key={name} variant="caption" display="block">
              {`${name}: ${active ? 'Syncing' : 'Synced'}`}
            </Typography>
          ))}
        </Box>
      }
    >
      <Chip
        icon={getStatusIcon() as React.ReactElement}
        label={getStatusLabel()}
        color={getStatusColor()}
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
