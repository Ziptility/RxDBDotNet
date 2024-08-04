import React, { useState, useEffect } from 'react';
import { Box, Switch, Typography, Chip } from '@mui/material';
import { Wifi as WifiIcon, WifiOff as WifiOffIcon, Sync as SyncIcon } from '@mui/icons-material';
import { RxReplicationState, DocType, Checkpoint } from '@/types';
import { combineLatest } from 'rxjs';

interface NetworkStatusProps {
  replicationStates: RxReplicationState<DocType, Checkpoint>[];
}

const NetworkStatus: React.FC<NetworkStatusProps> = ({ replicationStates }) => {
  const [isOnline, setIsOnline] = useState(navigator.onLine);
  const [isSyncing, setIsSyncing] = useState(false);

  useEffect(() => {
    const handleOnline = () => setIsOnline(true);
    const handleOffline = () => setIsOnline(false);

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  useEffect(() => {
    if (replicationStates.length === 0) return;

    const subscription = combineLatest(replicationStates.map(state => state.active$))
      .subscribe(activeStates => {
        const anySyncing = activeStates.some(isActive => isActive);
        setIsSyncing(anySyncing);
      });

    return () => subscription.unsubscribe();
  }, [replicationStates]);

  const toggleOnlineStatus = () => {
    if (isOnline) {
      // Go offline
      replicationStates.forEach(state => state.cancel());
      setIsOnline(false);
    } else {
      // Go online
      replicationStates.forEach(state => state.reSync());
      setIsOnline(true);
    }
  };

  return (
    <Box display="flex" alignItems="center" gap={2}>
      <Switch
        checked={isOnline}
        onChange={toggleOnlineStatus}
        color="primary"
        inputProps={{ 'aria-label': 'toggle online/offline status' }}
      />
      <Typography>
        {isOnline ? 'Online' : 'Offline'}
      </Typography>
      {isOnline && (
        <Chip
          icon={isSyncing ? <SyncIcon /> : <WifiIcon />}
          label={isSyncing ? 'Syncing' : 'Synced'}
          color={isSyncing ? 'warning' : 'success'}
          size="small"
        />
      )}
      {!isOnline && (
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