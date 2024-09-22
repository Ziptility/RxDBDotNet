// src\components\WorkspaceForm.tsx
import React, { useEffect } from 'react';
import { TextField, Button } from '@mui/material';
import { useForm, Controller } from 'react-hook-form';
import { FormLayout } from '@/components/FormComponents';
import type { Workspace } from '@/lib/schemas';

interface WorkspaceFormProps {
  readonly workspace: Workspace | undefined;
  readonly onSubmit: (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>) => void;
  readonly onCancel: () => void;
}

const WorkspaceForm: React.FC<WorkspaceFormProps> = ({ workspace, onSubmit, onCancel }) => {
  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting, isValid },
    reset,
  } = useForm({
    defaultValues: {
      name: '',
    },
    mode: 'onChange',
  });

  useEffect(() => {
    if (workspace) {
      reset({
        name: workspace.name,
      });
    } else {
      reset({
        name: '',
      });
    }
  }, [workspace, reset]);

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
        name="name"
        control={control}
        rules={{ required: 'Workspace name is required' }}
        render={({ field }) => (
          <TextField
            {...field}
            label="Workspace Name"
            error={!!errors.name}
            helperText={errors.name?.message}
            fullWidth
          />
        )}
      />
      <Button type="submit" variant="contained" color="primary" disabled={isSubmitting || !isValid} fullWidth>
        {workspace ? 'Update Workspace' : 'Create Workspace'}
      </Button>
      {workspace ? (
        <Button onClick={onCancel} variant="outlined" color="secondary" fullWidth sx={{ mt: 2 }}>
          Cancel
        </Button>
      ) : null}
    </FormLayout>
  );
};

export default WorkspaceForm;
