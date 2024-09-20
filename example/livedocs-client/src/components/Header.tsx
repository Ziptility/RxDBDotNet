import React from 'react';
import { AppBar, Toolbar, Typography, Box } from '@mui/material';
import dynamic from 'next/dynamic';
import { useRouter } from 'next/router';
import { useAuth } from '@/contexts/AuthContext';
import { SecondaryButton, SpaceBetweenBox } from '@/styles/StyledComponents';
import { NavButtons } from './NavButtons';

const NetworkStatus = dynamic(() => import('./NetworkStatus'), { ssr: false });

export const Header: React.FC = () => {
  const { logout } = useAuth();
  const router = useRouter();

  const handleLogout = async (): Promise<void> => {
    await logout();
    await router.push('/login');
  };

  return (
    <AppBar
      position="static"
      sx={{ bgcolor: 'background.paper', boxShadow: 'none', borderBottom: 1, borderColor: 'divider' }}
    >
      <Toolbar>
        <Typography variant="h6" component="div" sx={{ color: 'primary.main' }}>
          LiveDocs
        </Typography>
        <SpaceBetweenBox sx={{ ml: 2, flexGrow: 1 }}>
          <NavButtons />
          <Box sx={{ display: 'flex', alignItems: 'center' }}>
            <NetworkStatus />
            <SecondaryButton onClick={(): void => void handleLogout()} sx={{ ml: 2 }}>
              Logout
            </SecondaryButton>
          </Box>
        </SpaceBetweenBox>
      </Toolbar>
    </AppBar>
  );
};
