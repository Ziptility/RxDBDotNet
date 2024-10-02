// src/pages/_app.tsx

import { useEffect } from 'react';
import { CacheProvider } from '@emotion/react';
import CssBaseline from '@mui/material/CssBaseline';
import { ThemeProvider } from '@mui/material/styles';
import type { AppProps } from 'next/app';
import ErrorBoundary from '@/components/ErrorBoundary';
import ProtectedRoute from '@/components/ProtectedRoute';
import { AuthProvider } from '@/contexts/AuthContext';
import Layout from '../components/Layout';
import { createEmotionCache } from '../createEmotionCache';
import { theme } from '../theme';
import type { EmotionCache } from '@emotion/cache';
import '@/styles/globals.css';

interface MyAppProps extends AppProps {
  readonly emotionCache: EmotionCache;
}

const clientSideEmotionCache = createEmotionCache();

const MyApp = ({ Component, router, pageProps, emotionCache = clientSideEmotionCache }: MyAppProps): JSX.Element => {
  useEffect(() => {
    console.log('Application started');
  }, []);

  return (
    <CacheProvider value={emotionCache}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <ErrorBoundary>
          <AuthProvider>
            {router.pathname === '/login' ? (
              <Component {...pageProps} />
            ) : (
              <ProtectedRoute>
                <Layout>
                  <Component {...pageProps} />
                </Layout>
              </ProtectedRoute>
            )}
          </AuthProvider>
        </ErrorBoundary>
      </ThemeProvider>
    </CacheProvider>
  );
};

export default MyApp;
