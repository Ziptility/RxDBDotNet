import React, { createContext, useContext, useState, useEffect } from 'react';
import { User, Workspace } from '@/lib/schemas';
import { getDatabase } from '@/lib/database';
import { RxDocument } from 'rxdb';

interface AuthContextType {
  currentUser: User | null;
  currentWorkspace: Workspace | null;
  jwtAccessToken: string | null;
  isLoggedIn: boolean;
  login: (userId: string, workspaceId: string) => Promise<void>;
  logout: () => void;
  workspaces: Workspace[];
  users: User[];
  fetchWorkspaces: () => Promise<void>;
  fetchUsers: (workspaceId: string) => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [currentUser, setCurrentUser] = useState<User | null>(null);
  const [currentWorkspace, setCurrentWorkspace] = useState<Workspace | null>(null);
  const [jwtAccessToken, setJwtAccessToken] = useState<string | null>(null);
  const [isLoggedIn, setIsLoggedIn] = useState<boolean>(false);
  const [workspaces, setWorkspaces] = useState<Workspace[]>([]);
  const [users, setUsers] = useState<User[]>([]);

  useEffect(() => {
    const initializeAuth = async (): Promise<void> => {
      const storedUserId = localStorage.getItem('userId');
      const storedWorkspaceId = localStorage.getItem('workspaceId');
      if (storedUserId && storedWorkspaceId) {
        await login(storedUserId, storedWorkspaceId);
      }
    };

    void initializeAuth();
  }, []);

  const fetchWorkspaces = async (): Promise<void> => {
    const db = await getDatabase();
    const workspaces = (await db.workspaces.find().exec()) as Workspace[];
    setWorkspaces(workspaces);
  };

  const fetchUsers = async (workspaceId: string): Promise<void> => {
    const db = await getDatabase();
    const users = await db.users
      .find({
        selector: {
          workspaceId: workspaceId,
        },
      })
      .exec();
    setUsers(users);
  };

  const login = async (userId: string, workspaceId: string): Promise<void> => {
    const db = await getDatabase();
    const user: RxDocument<User> | null = (await db.users
      .findOne({
        selector: {
          id: userId,
        },
      })
      .exec()) as RxDocument<User> | null;

    const workspace: RxDocument<Workspace> | null = (await db.workspaces
      .findOne({
        selector: {
          id: workspaceId,
        },
      })
      .exec()) as RxDocument<Workspace> | null;

    if (user && workspace) {
      const userJson = user.toJSON();
      const workspaceJson = workspace.toJSON();

      localStorage.setItem('userId', userId);
      localStorage.setItem('workspaceId', workspaceId);
      localStorage.setItem('jwtAccessToken', user.jwtAccessToken ?? '');

      setCurrentUser(userJson);
      setCurrentWorkspace(workspaceJson);
      setJwtAccessToken(user.jwtAccessToken ?? '');
      setIsLoggedIn(true);
    } else {
      throw new Error('Invalid user or workspace');
    }
  };

  const logout = (): void => {
    localStorage.removeItem('userId');
    localStorage.removeItem('workspaceId');
    localStorage.removeItem('jwtAccessToken');
    setCurrentUser(null);
    setCurrentWorkspace(null);
    setJwtAccessToken(null);
    setIsLoggedIn(false);
  };

  return (
    <AuthContext.Provider
      value={{
        currentUser,
        currentWorkspace,
        jwtAccessToken,
        isLoggedIn,
        login,
        logout,
        workspaces,
        users,
        fetchWorkspaces,
        fetchUsers,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
