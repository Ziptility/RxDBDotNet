// src\pages\_app.tsx
import React from 'react';
import { CacheProvider } from '@emotion/react';
import CssBaseline from '@mui/material/CssBaseline';
import { ThemeProvider } from '@mui/material/styles';
import { ToastContainer } from 'react-toastify';
import type { AppProps } from 'next/app';
import 'react-toastify/dist/ReactToastify.css';
import ErrorBoundary from '@/components/ErrorBoundary';
import ProtectedRoute from '@/components/ProtectedRoute';
import { AuthProvider } from '@/contexts/AuthContext';
import Layout from '../components/Layout';
import createEmotionCache from '../createEmotionCache';
import theme from '../theme';
import type { EmotionCache } from '@emotion/cache';

interface MyAppProps extends AppProps {
  emotionCache?: EmotionCache;
}

const clientSideEmotionCache = createEmotionCache();

function MyApp({ Component, pageProps, router, emotionCache = clientSideEmotionCache }: MyAppProps): JSX.Element {
  return (
    <CacheProvider value={emotionCache}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <ErrorBoundary>
          <AuthProvider>
            <ToastContainer />
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
}

export default MyApp;
