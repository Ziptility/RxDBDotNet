// src\pages\_app.tsx
import { useEffect } from 'react';
import { CacheProvider } from '@emotion/react';
import CssBaseline from '@mui/material/CssBaseline';
import { ThemeProvider } from '@mui/material/styles';
import { ToastContainer } from 'react-toastify';
import type { AppProps } from 'next/app';
import 'react-toastify/dist/ReactToastify.css';
import ErrorBoundary from '@/components/ErrorBoundary';
import ProtectedRoute from '@/components/ProtectedRoute';
import { AuthProvider } from '@/contexts/AuthContext';
import { setupGlobalErrorListeners } from '@/utils/errorHandling';
import Layout from '../components/Layout';
import { createEmotionCache } from '../createEmotionCache';
import { theme } from '../theme';
import type { EmotionCache } from '@emotion/cache';
import '@/styles/globals.css';
interface MyAppProps extends AppProps {
  readonly emotionCache: EmotionCache;
}

const clientSideEmotionCache = createEmotionCache();

const MyApp = ({ Component, router, emotionCache = clientSideEmotionCache }: MyAppProps): JSX.Element => {
  useEffect(() => {
    setupGlobalErrorListeners();
  }, []);

  return (
    <CacheProvider value={emotionCache}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <ErrorBoundary>
          <AuthProvider>
            <ToastContainer />
            {router.pathname === '/login' ? (
              <Component key={router.route} />
            ) : (
              <ProtectedRoute>
                <Layout>
                  <Component key={router.route} />
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
