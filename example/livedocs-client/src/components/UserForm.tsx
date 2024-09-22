// src/components/UserForm.tsx
import React, { useEffect } from 'react';
import { TextField, MenuItem, FormControl, InputLabel, Select, Button } from '@mui/material';
import { useForm, Controller } from 'react-hook-form';
import { FormLayout, FormError } from '@/components/FormComponents';
import type { User, Workspace } from '@/lib/schemas';
import { UserRole } from '@/lib/schemas';

interface UserFormProps {
  readonly user: User | undefined;
  readonly workspaces: Workspace[];
  readonly onSubmit: (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>) => void;
  readonly onCancel: () => void;
}

const UserForm: React.FC<UserFormProps> = ({ user, workspaces, onSubmit, onCancel }) => {
  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting, isValid },
    reset,
  } = useForm({
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      role: UserRole.StandardUser,
      workspaceId: '',
    },
    mode: 'onChange',
  });

  useEffect(() => {
    if (user) {
      reset({
        firstName: user.firstName,
        lastName: user.lastName,
        email: user.email,
        role: user.role,
        workspaceId: user.workspaceId,
      });
    } else {
      reset({
        firstName: '',
        lastName: '',
        email: '',
        role: UserRole.StandardUser,
        workspaceId: '',
      });
    }
  }, [user, reset]);

  const onSubmitForm = handleSubmit((data) => {
    onSubmit(data);
  });

  return (
    <FormLayout
      title=""
      onSubmit={(e) => {
        e.preventDefault();
        void onSubmitForm();
      }}
    >
      <Controller
        name="firstName"
        control={control}
        rules={{ required: 'First name is required' }}
        render={({ field }) => (
          <TextField
            {...field}
            label="First Name"
            error={!!errors.firstName}
            helperText={errors.firstName?.message}
            fullWidth
          />
        )}
      />
      <Controller
        name="lastName"
        control={control}
        rules={{ required: 'Last name is required' }}
        render={({ field }) => (
          <TextField
            {...field}
            label="Last Name"
            error={!!errors.lastName}
            helperText={errors.lastName?.message}
            fullWidth
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
          <TextField
            {...field}
            label="Email"
            type="email"
            error={!!errors.email}
            helperText={errors.email?.message}
            fullWidth
          />
        )}
      />
      <Controller
        name="role"
        control={control}
        rules={{ required: 'Role is required' }}
        render={({ field }) => (
          <FormControl fullWidth error={!!errors.role}>
            <InputLabel>Role</InputLabel>
            <Select {...field} label="Role">
              {Object.values(UserRole).map((roleValue) => (
                <MenuItem key={roleValue} value={roleValue}>
                  {roleValue}
                </MenuItem>
              ))}
            </Select>
            {errors.role ? <FormError error={errors.role.message ?? null} /> : null}
          </FormControl>
        )}
      />
      <Controller
        name="workspaceId"
        control={control}
        rules={{ required: 'Workspace is required' }}
        render={({ field }) => (
          <FormControl fullWidth error={!!errors.workspaceId}>
            <InputLabel>Workspace</InputLabel>
            <Select {...field} label="Workspace">
              {workspaces.map((workspace) => (
                <MenuItem key={workspace.id} value={workspace.id}>
                  {workspace.name}
                </MenuItem>
              ))}
            </Select>
            {errors.workspaceId ? <FormError error={errors.workspaceId.message ?? null} /> : null}
          </FormControl>
        )}
      />
      <Button type="submit" variant="contained" color="primary" disabled={isSubmitting || !isValid} fullWidth>
        {user ? 'Update User' : 'Create User'}
      </Button>
      {user ? (
        <Button onClick={onCancel} variant="outlined" color="secondary" fullWidth sx={{ mt: 2 }}>
          Cancel
        </Button>
      ) : null}
    </FormLayout>
  );
};

export default UserForm;
