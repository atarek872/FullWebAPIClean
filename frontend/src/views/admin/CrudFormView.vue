<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import BaseCard from '@/components/common/BaseCard.vue';
import { adminModulesMap, type AdminModuleKey } from '@/modules/adminModules';
import { useAdminCrudStore } from '@/stores/adminCrud';

const route = useRoute();
const router = useRouter();
const store = useAdminCrudStore();

const moduleKey = computed(() => route.meta.moduleKey as AdminModuleKey);
const moduleDef = computed(() => adminModulesMap[moduleKey.value]);
const id = computed(() => String(route.params.id ?? ''));
const isEdit = computed(() => route.name?.toString().endsWith('-edit'));

const form = reactive<Record<string, unknown>>({});
const errors = ref<Record<string, string>>({});

const hydrateForm = () => {
  Object.assign(form, store.buildInitialForm(moduleKey.value));

  if (isEdit.value) {
    const record = store.getById(moduleKey.value, id.value);
    if (record) {
      Object.assign(form, record);
    }
  }
};

onMounted(async () => {
  if (!store.byModule[moduleKey.value].length) {
    await store.load(moduleKey.value);
  }
  hydrateForm();
});

const validate = () => {
  const next: Record<string, string> = {};

  moduleDef.value.fields.forEach((field) => {
    if (field.required && !String(form[field.key] ?? '').trim()) {
      next[field.key] = `${field.label} is required`;
    }
  });

  errors.value = next;
  return Object.keys(next).length === 0;
};

const onSubmit = async () => {
  if (!validate()) {
    return;
  }

  if (isEdit.value) {
    await store.update(moduleKey.value, id.value, { ...form });
  } else {
    await store.create(moduleKey.value, { ...form });
  }

  router.push(moduleDef.value.routeBase);
};
</script>

<template>
  <BaseCard>
    <h2 style="margin-top:0">{{ moduleDef.title }} - {{ isEdit ? 'Edit' : 'Create' }}</h2>
    <p v-if="store.error" style="color:#b91c1c;">{{ store.error }}</p>

    <form @submit.prevent="onSubmit" style="display:grid;gap:0.75rem;">
      <label v-for="field in moduleDef.fields" :key="field.key" style="display:grid;gap:0.25rem;">
        <span>{{ field.label }}</span>
        <input
          v-if="field.type !== 'textarea' && field.type !== 'checkbox'"
          v-model="form[field.key]"
          class="input"
          :type="field.type ?? 'text'"
        />
        <textarea v-else-if="field.type === 'textarea'" v-model="form[field.key]" class="input" rows="3" />
        <input v-else v-model="form[field.key]" type="checkbox" style="width:20px;height:20px;" />
        <small v-if="errors[field.key]" style="color:#b91c1c;">{{ errors[field.key] }}</small>
      </label>

      <div style="display:flex;gap:0.5rem;justify-content:flex-end;">
        <RouterLink :to="moduleDef.routeBase" class="btn" style="text-decoration:none;">Cancel</RouterLink>
        <button class="btn btn-primary" :disabled="store.loading" type="submit">{{ isEdit ? 'Update' : 'Create' }}</button>
      </div>
    </form>
  </BaseCard>
</template>
