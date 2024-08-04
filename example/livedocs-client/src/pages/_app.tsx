import React, { useEffect, useState } from 'react';
import { AppProps } from 'next/app';
import { ThemeProvider } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import theme from '../theme';
import Layout from '@/components/Layout';
import { getDatabase } from '@/lib/database';
import { setupReplication } from '@/lib/replication';
import { RxReplicationState } from '@/types';

function MyApp({ Component, pageProps }: AppProps) {
  const [replicationStates, setReplicationStates] = useState<RxReplicationState[]>([]);

  useEffect(() => {
    const initDb = async () => {
      const db = await getDatabase();
      const states = await setupReplication(db);
      setReplicationStates(states);
    };
    initDb();
  }, []);

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Layout replicationStates={replicationStates}>
        <Component {...pageProps} />
      </Layout>
    </ThemeProvider>
  );
}

export default MyApp;