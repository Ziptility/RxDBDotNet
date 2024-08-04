import React, { useState, useEffect } from 'react';
import { Box, Typography, Chip } from '@mui/material';
import { Wifi as WifiIcon, WifiOff as WifiOffIcon, Sync as SyncIcon } from '@mui/icons-material';
import { useOnlineStatus } from '@/hooks/useOnlineStatus';
import { getDatabase } from '@/lib/database';
import { setupReplication } from '@/lib/replication';
import { RxReplicationState, LiveDocsDocType, ReplicationCheckpoint } from '@/types';
import { combineLatest } from 'rxjs';

const NetworkStatus: React.FC = () => {
  const isOnline = useOnlineStatus();
  const [isSyncing, setIsSyncing] = useState(false);
  const [replicationStates, setReplicationStates] = useState<RxReplicationState<LiveDocsDocType, ReplicationCheckpoint>[]>([]);

  useEffect(() => {
    const initReplication = async () => {
      const db = await getDatabase();
      const states = await setupReplication(db);
      setReplicationStates(states);
    };

    initReplication();
  }, []);

  useEffect(() => {
    if (replicationStates.length === 0) return;

    const subscription = combineLatest(replicationStates.map(state => state.active$))
      .subscribe(activeStates => {
        setIsSyncing(activeStates.some(Boolean));
      });

    return () => subscription.unsubscribe();
  }, [replicationStates]);

  return (
    <Box display="flex" alignItems="center" gap={2}>
      <Typography>
        {isOnline ? 'Online' : 'Offline'}
      </Typography>
      {isOnline ? (
        <Chip
          icon={isSyncing ? <SyncIcon /> : <WifiIcon />}
          label={isSyncing ? 'Syncing' : 'Synced'}
          color={isSyncing ? 'warning' : 'success'}
          size="small"
        />
      ) : (
        <Chip
          icon={<WifiOffIcon />}
          label="Offline"
          color="error"
          size="small"
        />
      )}
    </Box>
  );
};

export default NetworkStatus;