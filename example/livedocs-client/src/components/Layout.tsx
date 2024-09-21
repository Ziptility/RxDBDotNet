// src/components/Layout.tsx
import React, { type ReactNode } from 'react';
import { Box } from '@mui/material';
import { motion } from 'framer-motion';
import { useRouter } from 'next/router';
import { PageContainer } from '@/styles/StyledComponents';
import { motionProps, AnimatePresence } from '@/utils/motionSystem';
import { Footer } from './Footer';
import NavigationRail from './NavigationRail';

interface LayoutProps {
  readonly children: ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }): JSX.Element => {
  const router = useRouter();

  return (
    <Box
      sx={{
        display: 'flex',
        minHeight: '100vh',
        bgcolor: 'background.default',
      }}
    >
      <NavigationRail />
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          ml: '80px', // Adjust to match the width of the NavigationRail
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <Box sx={{ flexGrow: 1 }}>
          <AnimatePresence mode="wait">
            <motion.div key={router.pathname} {...motionProps['fadeIn']}>
              <PageContainer>{children}</PageContainer>
            </motion.div>
          </AnimatePresence>
        </Box>
        <Footer />
      </Box>
    </Box>
  );
};

export default Layout;
