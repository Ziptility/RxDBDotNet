import React, { useState, useEffect, useCallback, useRef } from 'react';
import type { ReactNode } from 'react';
import { API_CONFIG } from '@/config';
import { useDocuments } from '@/hooks/useDocuments';
import { getDatabase } from '@/lib/database';
import { setupReplication, restartReplication, cancelReplication } from '@/lib/replication';
import type { User, Workspace } from '@/lib/schemas';
import type { LiveDocsDatabase } from '@/types';
import { createTypedContext } from '@/utils/createTypedContext';
import { handleAsyncError } from '@/utils/errorHandling';

interface AuthContextType {
  currentUser: User | null;
  currentWorkspace: Workspace | null;
  jwtAccessToken: string | null;
  isLoggedIn: boolean;
  isInitialized: boolean;
  login: (userId: string, workspaceId: string) => Promise<void>;
  logout: () => Promise<void>;
  workspaces: Workspace[];
  users: User[];
}

const [useAuth, AuthProvider] = createTypedContext<AuthContextType>();

interface AuthProviderComponentProps {
  readonly children: ReactNode;
}

const AuthProviderComponent: React.FC<AuthProviderComponentProps> = ({ children }): JSX.Element => {
  const [currentUser, setCurrentUser] = useState<User | null>(null);
  const [currentWorkspace, setCurrentWorkspace] = useState<Workspace | null>(null);
  const [jwtAccessToken, setJwtAccessToken] = useState<string | null>(null);
  const [isLoggedIn, setIsLoggedIn] = useState<boolean>(false);
  const [isInitialized, setIsInitialized] = useState<boolean>(false);

  const { documents: workspaces, isLoading: isLoadingWorkspaces } = useDocuments<Workspace>('workspace');
  const { documents: users, isLoading: isLoadingUsers } = useDocuments<User>('user');

  const loginRef = useRef<AuthContextType['login']>();
  const logoutRef = useRef<AuthContextType['logout']>();

  const setToken = useCallback((token: string) => {
    setJwtAccessToken(token);
    localStorage.setItem('jwtAccessToken', token);
  }, []);

  const setupOrRestartReplication = useCallback(async (db: LiveDocsDatabase, token: string) => {
    if (db.replicationStates) {
      await restartReplication(db.replicationStates);
    } else {
      db.replicationStates = await setupReplication(db, token);
    }
  }, []);

  loginRef.current = useCallback(
    async (userId: string, workspaceId: string): Promise<void> => {
      await handleAsyncError(async () => {
        const user = users.find((u) => u.id === userId);
        const workspace = workspaces.find((w) => w.id === workspaceId);

        if (!user || !workspace) {
          throw new Error('Invalid user or workspace');
        }

        setCurrentUser(user);
        setCurrentWorkspace(workspace);
        const token = user.jwtAccessToken ?? '';
        setToken(token);
        setIsLoggedIn(true);

        localStorage.setItem('userId', userId);
        localStorage.setItem('workspaceId', workspaceId);

        const db: LiveDocsDatabase = await getDatabase();
        await setupOrRestartReplication(db, token);
      }, 'Login');
    },
    [users, workspaces, setToken, setupOrRestartReplication]
  );

  logoutRef.current = useCallback(async (): Promise<void> => {
    setCurrentUser(null);
    setCurrentWorkspace(null);
    setIsLoggedIn(false);

    localStorage.removeItem('userId');
    localStorage.removeItem('workspaceId');
    localStorage.removeItem('jwtAccessToken');

    const db: LiveDocsDatabase = await getDatabase();
    if (db.replicationStates) {
      await cancelReplication(db.replicationStates);
    }

    const defaultToken = API_CONFIG.DEFAULT_JWT_TOKEN;
    setToken(defaultToken);
    await setupOrRestartReplication(db, defaultToken);
  }, [setToken, setupOrRestartReplication]);

  const initializeAuth = useCallback(async (): Promise<void> => {
    const storedUserId = localStorage.getItem('userId');
    const storedWorkspaceId = localStorage.getItem('workspaceId');
    const storedJwtToken = localStorage.getItem('jwtAccessToken');

    const db: LiveDocsDatabase = await getDatabase();

    if (
      typeof storedUserId === 'string' &&
      storedUserId.length > 0 &&
      typeof storedWorkspaceId === 'string' &&
      storedWorkspaceId.length > 0 &&
      typeof storedJwtToken === 'string' &&
      storedJwtToken.length > 0 &&
      loginRef.current
    ) {
      try {
        await loginRef.current(storedUserId, storedWorkspaceId);
      } catch (error) {
        console.error('Failed to initialize authentication:', error);
        if (logoutRef.current) {
          await logoutRef.current();
        }
      }
    } else {
      console.log('No valid stored authentication data found. Using default token.');
      const defaultToken = API_CONFIG.DEFAULT_JWT_TOKEN;
      setToken(defaultToken);
      await setupOrRestartReplication(db, defaultToken);
    }

    setIsInitialized(true);
  }, [setToken, setupOrRestartReplication]);

  useEffect(() => {
    void initializeAuth();
  }, [initializeAuth]);

  const contextValue: AuthContextType = {
    currentUser,
    currentWorkspace,
    jwtAccessToken,
    isLoggedIn,
    isInitialized,
    login: useCallback((userId: string, workspaceId: string) => {
      if (loginRef.current) {
        return loginRef.current(userId, workspaceId);
      }
      return Promise.resolve();
    }, []),
    logout: useCallback(async () => {
      if (logoutRef.current) {
        await logoutRef.current();
      }
    }, []),
    workspaces,
    users,
  };

  if (isLoadingWorkspaces || isLoadingUsers) {
    return <div>Loading...</div>;
  }

  return <AuthProvider value={contextValue}>{children}</AuthProvider>;
};

export { useAuth, AuthProviderComponent as AuthProvider };
