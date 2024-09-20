import React, { type ReactNode } from 'react';
import { Box } from '@mui/material';
import { PageContainer } from '@/styles/StyledComponents';
import { Footer } from './Footer';
import { Header } from './Header';

interface LayoutProps {
  readonly children: ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }): JSX.Element => {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        minHeight: '100vh',
        bgcolor: 'background.default',
      }}
    >
      <Header />
      <Box component="main" sx={{ flexGrow: 1 }}>
        <PageContainer sx={{ py: 3 }}>{children}</PageContainer>
      </Box>
      <Footer />
    </Box>
  );
};

export default Layout;
