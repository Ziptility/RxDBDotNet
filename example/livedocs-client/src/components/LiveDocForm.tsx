// example/livedocs-client/src/components/LiveDocForm.tsx
import React, { useEffect } from 'react';
import { TextField, MenuItem, FormControl, InputLabel, Select, Button } from '@mui/material';
import { styled } from '@mui/material/styles';
import { useForm, Controller } from 'react-hook-form';
import { FormLayout, FormError } from '@/components/FormComponents';
import type { LiveDoc, User, Workspace } from '@/generated/graphql';

const StyledTextField = styled(TextField)(({ theme }) => ({
  '& .MuiInputBase-root': {
    backgroundColor: theme.palette.background.paper,
  },
}));

const StyledFormControl = styled(FormControl)(({ theme }) => ({
  minWidth: 120,
  backgroundColor: theme.palette.background.paper,
}));

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
  } = useForm<Omit<LiveDoc, 'id' | 'updatedAt' | 'isDeleted'>>({
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
        void onSubmitForm(e);
      }}
    >
      <Controller
        name="content"
        control={control}
        rules={{ required: 'Content is required' }}
        render={({ field }) => (
          <StyledTextField
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
          <StyledFormControl fullWidth error={!!errors.ownerId}>
            <InputLabel>Owner</InputLabel>
            <Select {...field} label="Owner">
              {users.map((user) => (
                <MenuItem key={user.id} value={user.id}>
                  {`${user.firstName} ${user.lastName}`}
                </MenuItem>
              ))}
            </Select>
            {errors.ownerId ? <FormError error={errors.ownerId.message ?? null} /> : null}
          </StyledFormControl>
        )}
      />
      <Controller
        name="workspaceId"
        control={control}
        rules={{ required: 'Workspace is required' }}
        render={({ field }) => (
          <StyledFormControl fullWidth error={!!errors.workspaceId}>
            <InputLabel>Workspace</InputLabel>
            <Select {...field} label="Workspace">
              {workspaces.map((workspace) => (
                <MenuItem key={workspace.id} value={workspace.id}>
                  {workspace.name}
                </MenuItem>
              ))}
            </Select>
            {errors.workspaceId ? <FormError error={errors.workspaceId.message ?? null} /> : null}
          </StyledFormControl>
        )}
      />
      <Button type="submit" variant="contained" color="primary" disabled={isSubmitting || !isValid} fullWidth>
        {liveDoc ? 'Update LiveDoc' : 'Create LiveDoc'}
      </Button>
      <Button onClick={onCancel} variant="outlined" color="secondary" fullWidth sx={{ mt: 2 }}>
        Cancel
      </Button>
    </FormLayout>
  );
};

export default LiveDocForm;
