// example/livedocs-client/src/components/UserForm.tsx
import React, { useEffect } from 'react';
import { TextField, Button, FormControl, InputLabel, Select, MenuItem } from '@mui/material';
import { styled } from '@mui/material/styles';
import { useForm, Controller } from 'react-hook-form';
import { FormLayout } from '@/components/FormComponents';
import { type User, type Workspace, UserRole } from '@/generated/graphql';

const StyledTextField = styled(TextField)(({ theme }) => ({
  '& .MuiInputBase-root': {
    backgroundColor: theme.palette.background.paper,
  },
}));

const StyledFormControl = styled(FormControl)(({ theme }) => ({
  minWidth: 120,
  backgroundColor: theme.palette.background.paper,
}));

interface UserFormProps {
  readonly user: User | null;
  readonly workspaces: Workspace[];
  readonly onSubmit: (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>) => void;
  readonly onCancel: () => void;
  readonly isInline: boolean;
}

const UserForm: React.FC<UserFormProps> = ({ user, workspaces, onSubmit, onCancel, isInline = false }) => {
  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting, isValid },
    reset,
  } = useForm<Omit<User, 'id' | 'updatedAt' | 'isDeleted'>>({
    defaultValues: {
      firstName: user?.firstName ?? '',
      lastName: user?.lastName ?? '',
      email: user?.email ?? '',
      role: user?.role ?? UserRole.StandardUser,
      workspaceId: user?.workspaceId ?? '',
    },
    mode: 'onChange',
  });

  useEffect(() => {
    reset({
      firstName: user?.firstName ?? '',
      lastName: user?.lastName ?? '',
      email: user?.email ?? '',
      role: user?.role ?? UserRole.StandardUser,
      workspaceId: user?.workspaceId ?? '',
    });
  }, [user, reset]);

  const onSubmitForm = handleSubmit((data) => {
    onSubmit(data);
  });

  const formContent = (
    <>
      <Controller
        name="firstName"
        control={control}
        rules={{ required: 'First name is required' }}
        render={({ field }) => (
          <StyledTextField
            {...field}
            label="First Name"
            error={!!errors.firstName}
            helperText={errors.firstName?.message}
            size={isInline ? 'small' : 'medium'}
            variant={isInline ? 'outlined' : 'filled'}
            slotProps={{
              input: {
                inputProps: {
                  maxLength: 50,
                },
              },
            }}
            sx={{
              width: 'auto',
              '& .MuiInputBase-input': {
                width: '20ch',
              },
            }}
          />
        )}
      />
      <Controller
        name="lastName"
        control={control}
        rules={{ required: 'Last name is required' }}
        render={({ field }) => (
          <StyledTextField
            {...field}
            label="Last Name"
            error={!!errors.lastName}
            helperText={errors.lastName?.message}
            size={isInline ? 'small' : 'medium'}
            variant={isInline ? 'outlined' : 'filled'}
            slotProps={{
              input: {
                inputProps: {
                  maxLength: 50,
                },
              },
            }}
            sx={{
              width: 'auto',
              '& .MuiInputBase-input': {
                width: '20ch',
              },
            }}
          />
        )}
      />
      <Controller
        name="email"
        control={control}
        rules={{
          required: 'Email is required',
          pattern: {
            value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
            message: 'Invalid email address',
          },
        }}
        render={({ field }) => (
          <StyledTextField
            {...field}
            label="Email"
            error={!!errors.email}
            helperText={errors.email?.message}
            size={isInline ? 'small' : 'medium'}
            variant={isInline ? 'outlined' : 'filled'}
            slotProps={{
              input: {
                inputProps: {
                  maxLength: 100,
                },
              },
            }}
            sx={{
              width: 'auto',
              '& .MuiInputBase-input': {
                width: '30ch',
              },
            }}
          />
        )}
      />
      <Controller
        name="role"
        control={control}
        rules={{ required: 'Role is required' }}
        render={({ field }) => (
          <StyledFormControl error={!!errors.role} size={isInline ? 'small' : 'medium'}>
            <InputLabel>Role</InputLabel>
            <Select {...field} label="Role">
              {Object.values(UserRole).map((role) => (
                <MenuItem key={role} value={role}>
                  {role}
                </MenuItem>
              ))}
            </Select>
          </StyledFormControl>
        )}
      />
      <Controller
        name="workspaceId"
        control={control}
        rules={{ required: 'Workspace is required' }}
        render={({ field }) => (
          <StyledFormControl error={!!errors.workspaceId} size={isInline ? 'small' : 'medium'}>
            <InputLabel>Workspace</InputLabel>
            <Select {...field} label="Workspace">
              {workspaces.map((workspace) => (
                <MenuItem key={workspace.id} value={workspace.id}>
                  {workspace.name}
                </MenuItem>
              ))}
            </Select>
          </StyledFormControl>
        )}
      />
      <Button
        type="submit"
        variant="contained"
        color="primary"
        disabled={isSubmitting || !isValid}
        size={isInline ? 'small' : 'medium'}
      >
        {user ? 'Update' : 'Create'}
      </Button>
      <Button onClick={onCancel} variant="outlined" color="secondary" size={isInline ? 'small' : 'medium'}>
        Cancel
      </Button>
    </>
  );

  return (
    <FormLayout
      title=""
      onSubmit={(e) => {
        void onSubmitForm(e);
      }}
    >
      {formContent}
    </FormLayout>
  );
};

export default UserForm;
