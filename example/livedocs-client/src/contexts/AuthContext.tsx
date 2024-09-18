import React, { useState, useEffect, useCallback, ReactNode, useRef } from 'react';
import { User, Workspace } from '@/lib/schemas';
import { useDocuments } from '@/hooks/useDocuments';
import { createTypedContext } from '@/utils/createTypedContext';
import { handleAsyncError } from '@/utils/errorHandling';

interface AuthContextType {
  currentUser: User | null;
  currentWorkspace: Workspace | null;
  jwtAccessToken: string | null;
  isLoggedIn: boolean;
  isInitialized: boolean;
  login: (userId: string, workspaceId: string) => Promise<void>;
  logout: () => void;
  workspaces: Workspace[];
  users: User[];
}

const [useAuth, AuthProvider] = createTypedContext<AuthContextType>();

interface AuthProviderComponentProps {
  children: ReactNode;
}

const AuthProviderComponent: React.FC<AuthProviderComponentProps> = ({ children }): JSX.Element => {
  const [currentUser, setCurrentUser] = useState<User | null>(null);
  const [currentWorkspace, setCurrentWorkspace] = useState<Workspace | null>(null);
  const [jwtAccessToken, setJwtAccessToken] = useState<string | null>(null);
  const [isLoggedIn, setIsLoggedIn] = useState<boolean>(false);
  const [isInitialized, setIsInitialized] = useState<boolean>(false);

  const { documents: workspaces, isLoading: isLoadingWorkspaces } = useDocuments<Workspace>('workspaces');

  const { documents: users, isLoading: isLoadingUsers } = useDocuments<User>('users');

  const loginRef = useRef<AuthContextType['login']>();
  const logoutRef = useRef<AuthContextType['logout']>();

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
        setJwtAccessToken(user.jwtAccessToken ?? null);
        setIsLoggedIn(true);

        localStorage.setItem('userId', userId);
        localStorage.setItem('workspaceId', workspaceId);
        localStorage.setItem('jwtAccessToken', user.jwtAccessToken ?? '');
        return Promise.resolve();
      }, 'Login');
    },
    [users, workspaces]
  );

  logoutRef.current = useCallback(() => {
    setCurrentUser(null);
    setCurrentWorkspace(null);
    setJwtAccessToken(null);
    setIsLoggedIn(false);

    localStorage.removeItem('userId');
    localStorage.removeItem('workspaceId');
    localStorage.removeItem('jwtAccessToken');
  }, []);

  const initializeAuth = useCallback(async (): Promise<void> => {
    const storedUserId = localStorage.getItem('userId');
    const storedWorkspaceId = localStorage.getItem('workspaceId');
    const storedJwtToken = localStorage.getItem('jwtAccessToken');

    if (storedUserId && storedWorkspaceId && storedJwtToken && loginRef.current) {
      try {
        await loginRef.current(storedUserId, storedWorkspaceId);
      } catch (error) {
        console.error('Failed to initialize authentication:', error);
        if (logoutRef.current) {
          logoutRef.current();
        }
      }
    }

    setIsInitialized(true);
  }, []);

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
    logout: useCallback(() => {
      if (logoutRef.current) {
        logoutRef.current();
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
