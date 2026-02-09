<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import BaseCard from '@/components/common/BaseCard.vue';
import DeleteModal from '@/components/admin/DeleteModal.vue';
import { adminModulesMap, type AdminModuleKey } from '@/modules/adminModules';
import { useAdminCrudStore } from '@/stores/adminCrud';

const route = useRoute();
const router = useRouter();
const store = useAdminCrudStore();

const moduleKey = computed(() => route.meta.moduleKey as AdminModuleKey);
const moduleDef = computed(() => adminModulesMap[moduleKey.value]);
const records = computed(() => store.getRecords(moduleKey.value).value);

const deletingId = ref<string | null>(null);

onMounted(async () => {
  await store.load(moduleKey.value);
});

const toCreate = () => router.push(`${moduleDef.value.routeBase}/create`);
const toEdit = (id: string) => router.push(`${moduleDef.value.routeBase}/${id}/edit`);

const openDelete = (id: string) => {
  deletingId.value = id;
};

const closeDelete = () => {
  deletingId.value = null;
};

const onDelete = async () => {
  if (!deletingId.value) {
    return;
  }

  await store.remove(moduleKey.value, deletingId.value);
  closeDelete();
};
</script>

<template>
  <BaseCard>
    <div style="display:flex;justify-content:space-between;align-items:center;gap:0.5rem;">
      <h2 style="margin:0;">{{ moduleDef.title }} - List</h2>
      <button class="btn btn-primary" @click="toCreate">Create {{ moduleDef.title }}</button>
    </div>

    <p v-if="store.error" style="color:#b91c1c;">{{ store.error }}</p>
    <p v-if="!records.length && !store.loading">No records yet.</p>

    <div v-if="records.length" style="overflow:auto;margin-top:1rem;">
      <table style="width:100%;border-collapse:collapse;">
        <thead>
          <tr>
            <th v-for="field in moduleDef.fields" :key="field.key" style="text-align:left;border-bottom:1px solid #e2e8f0;padding:0.5rem;">{{ field.label }}</th>
            <th style="text-align:right;border-bottom:1px solid #e2e8f0;padding:0.5rem;">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in records" :key="item.id">
            <td v-for="field in moduleDef.fields" :key="field.key" style="padding:0.5rem;border-bottom:1px solid #f1f5f9;">
              {{ item[field.key] }}
            </td>
            <td style="padding:0.5rem;border-bottom:1px solid #f1f5f9;text-align:right;">
              <button class="btn" @click="toEdit(String(item.id))">Edit</button>
              <button class="btn btn-danger" style="margin-left:0.5rem;" @click="openDelete(String(item.id))">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </BaseCard>

  <DeleteModal
    :open="Boolean(deletingId)"
    :title="`Delete ${moduleDef.title} record`"
    message="This action cannot be undone."
    :loading="store.loading"
    @cancel="closeDelete"
    @confirm="onDelete"
  />
</template>
