// src\components\LiveDocForm.tsx
import React, { useEffect } from 'react';
import { TextField, MenuItem, FormControl, InputLabel, Select, Button } from '@mui/material';
import { useForm, Controller } from 'react-hook-form';
import { FormLayout, FormError } from '@/components/FormComponents';
import type { LiveDoc, User, Workspace } from '@/generated/graphql';

interface LiveDocFormProps {
  readonly liveDoc: LiveDoc | undefined;
  readonly users: User[];
  readonly workspaces: Workspace[];
  readonly onSubmit: (liveDoc: Omit<LiveDoc, 'id' | 'updatedAt' | 'isDeleted'>) => void;
  readonly onCancel: () => void;
}

const LiveDocForm: React.FC<LiveDocFormProps> = ({ liveDoc, users, workspaces, onSubmit, onCancel }) => {
  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting, isValid },
    reset,
  } = useForm({
    defaultValues: {
      content: '',
      ownerId: '',
      workspaceId: '',
    },
    mode: 'onChange',
  });

  useEffect(() => {
    if (liveDoc) {
      reset({
        content: liveDoc.content,
        ownerId: liveDoc.ownerId,
        workspaceId: liveDoc.workspaceId,
      });
    } else {
      reset({
        content: '',
        ownerId: '',
        workspaceId: '',
      });
    }
  }, [liveDoc, reset]);

  const onSubmitForm = handleSubmit((data) => {
    onSubmit({ ...data, topics: [] });
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
        name="content"
        control={control}
        rules={{ required: 'Content is required' }}
        render={({ field }) => (
          <TextField
            {...field}
            label="Content"
            multiline
            rows={4}
            error={!!errors.content}
            helperText={errors.content?.message}
            fullWidth
          />
        )}
      />
      <Controller
        name="ownerId"
        control={control}
        rules={{ required: 'Owner is required' }}
        render={({ field }) => (
          <FormControl fullWidth error={!!errors.ownerId}>
            <InputLabel>Owner</InputLabel>
            <Select {...field} label="Owner">
              {users.map((user) => (
                <MenuItem key={user.id} value={user.id}>
                  {`${user.firstName} ${user.lastName}`}
                </MenuItem>
              ))}
            </Select>
            {errors.ownerId ? <FormError error={errors.ownerId.message ?? null} /> : null}
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
        {liveDoc ? 'Update LiveDoc' : 'Create LiveDoc'}
      </Button>
      {liveDoc ? (
        <Button onClick={onCancel} variant="outlined" color="secondary" fullWidth sx={{ mt: 2 }}>
          Cancel
        </Button>
      ) : null}
    </FormLayout>
  );
};

export default LiveDocForm;
