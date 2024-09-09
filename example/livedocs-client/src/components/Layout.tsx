import React, { ReactNode } from 'react';
import { AppBar, Toolbar, Typography, Container, Box, Button } from '@mui/material';
import Link from 'next/link';
import { useRouter } from 'next/router';
import dynamic from 'next/dynamic';
import { useAuth } from '@/contexts/AuthContext';

const NetworkStatus = dynamic(() => import('./NetworkStatus'), { ssr: false });

interface LayoutProps {
  children: ReactNode;
}

const navItems = [
  { label: 'Home', path: '/' },
  { label: 'Workspaces', path: '/workspaces' },
  { label: 'Users', path: '/users' },
  { label: 'LiveDocs', path: '/livedocs' },
];

const Layout: React.FC<LayoutProps> = ({ children }): JSX.Element => {
  const router = useRouter();
  const { logout } = useAuth();

  const handleLogout = (): void => {
    logout();
    void router.push('/login');
  };

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <AppBar position="static">
        <Toolbar>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            LiveDocs
          </Typography>
          <Box>
            {navItems.map((item) => (
              <Button
                key={item.path}
                color="inherit"
                component={Link}
                href={item.path}
                sx={{
                  textTransform: 'none',
                  fontWeight: router.pathname === item.path ? 'bold' : 'normal',
                  borderBottom: router.pathname === item.path ? '2px solid white' : 'none',
                }}
              >
                {item.label}
              </Button>
            ))}
          </Box>
          <Box ml={2}>
            <NetworkStatus />
          </Box>
          <Button color="inherit" onClick={handleLogout}>
            Logout
          </Button>
        </Toolbar>
      </AppBar>
      <Container component="main" sx={{ mt: 4, mb: 4, flexGrow: 1 }}>
        {children}
      </Container>
      <Box component="footer" sx={{ py: 3, px: 2, mt: 'auto', backgroundColor: 'background.paper' }}>
        <Container maxWidth="sm">
          <Typography variant="body2" color="text.secondary" align="center">
            © {new Date().getFullYear()} RxDBDotNet Contributors. All rights reserved.
          </Typography>
        </Container>
      </Box>
    </Box>
  );
};

export default Layout;
