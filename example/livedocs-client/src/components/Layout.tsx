import React, { ReactNode } from 'react';
import { AppBar, Toolbar, Typography, Container, Box } from '@mui/material';
import Link from 'next/link';

interface LayoutProps {
  children: ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <AppBar position="static">
        <Toolbar>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            LiveDocs
          </Typography>
          <Box sx={{ '& > *': { ml: 2 } }}>
            <Link href="/" passHref>
              <Typography component="a" color="inherit">Home</Typography>
            </Link>
            <Link href="/workspaces" passHref>
              <Typography component="a" color="inherit">Workspaces</Typography>
            </Link>
            <Link href="/users" passHref>
              <Typography component="a" color="inherit">Users</Typography>
            </Link>
            <Link href="/livedocs" passHref>
              <Typography component="a" color="inherit">LiveDocs</Typography>
            </Link>
          </Box>
        </Toolbar>
      </AppBar>
      <Container component="main" sx={{ mt: 4, mb: 4, flexGrow: 1 }}>
        {children}
      </Container>
      <Box component="footer" sx={{ py: 3, px: 2, mt: 'auto', backgroundColor: 'background.paper' }}>
        <Container maxWidth="sm">
          <Typography variant="body2" color="text.secondary" align="center">
            © {new Date().getFullYear()} LiveDocs. All rights reserved.
          </Typography>
        </Container>
      </Box>
    </Box>
  );
};

export default Layout;