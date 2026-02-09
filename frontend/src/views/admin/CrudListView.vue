<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
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
const filter = ref('');
const sortBy = ref('id');
const sortDirection = ref<'asc' | 'desc'>('asc');
const page = ref(1);
const pageSize = ref(10);

const canCreate = computed(() => Boolean(moduleDef.value.capabilities.create));
const canUpdate = computed(() => Boolean(moduleDef.value.capabilities.update));
const canRemove = computed(() => Boolean(moduleDef.value.capabilities.remove));
const canList = computed(() => Boolean(moduleDef.value.capabilities.list));

const filteredRecords = computed(() => {
  const q = filter.value.trim().toLowerCase();
  const base = q
    ? records.value.filter((item) =>
        moduleDef.value.fields.some((field) => String(item[field.key] ?? '').toLowerCase().includes(q))
      )
    : records.value;

  const key = sortBy.value;
  return [...base].sort((a, b) => {
    const aValue = String(a[key] ?? '').toLowerCase();
    const bValue = String(b[key] ?? '').toLowerCase();

    if (aValue === bValue) {
      return 0;
    }

    const result = aValue > bValue ? 1 : -1;
    return sortDirection.value === 'asc' ? result : -result;
  });
});

const totalPages = computed(() => Math.max(1, Math.ceil(filteredRecords.value.length / pageSize.value)));

const pagedRecords = computed(() => {
  const start = (page.value - 1) * pageSize.value;
  return filteredRecords.value.slice(start, start + pageSize.value);
});

watch(
  [filter, sortBy, sortDirection, pageSize],
  () => {
    page.value = 1;
  },
  { deep: true }
);

watch(totalPages, (value) => {
  if (page.value > value) {
    page.value = value;
  }
});

onMounted(async () => {
  if (canList.value) {
    await store.load(moduleKey.value);
    sortBy.value = moduleDef.value.fields[0]?.key ?? 'id';
  }
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
      <button v-if="canCreate" class="btn btn-primary" @click="toCreate">Create {{ moduleDef.title }}</button>
    </div>

    <p v-if="store.error" style="color:#b91c1c;">{{ store.error }}</p>
    <p v-if="!canList" style="margin-top:1rem;">This module only supports non-list actions in the current API.</p>

    <template v-if="canList">
      <div style="display:grid;grid-template-columns:1fr auto auto auto;gap:0.5rem;margin-top:1rem;align-items:end;">
        <label style="display:grid;gap:0.25rem;">
          <span>Filter</span>
          <input v-model="filter" class="input" type="text" placeholder="Filter rows" />
        </label>
        <label style="display:grid;gap:0.25rem;">
          <span>Sort By</span>
          <select v-model="sortBy" class="input">
            <option v-for="field in moduleDef.fields" :key="field.key" :value="field.key">{{ field.label }}</option>
          </select>
        </label>
        <label style="display:grid;gap:0.25rem;">
          <span>Direction</span>
          <select v-model="sortDirection" class="input">
            <option value="asc">Ascending</option>
            <option value="desc">Descending</option>
          </select>
        </label>
        <label style="display:grid;gap:0.25rem;">
          <span>Page Size</span>
          <select v-model.number="pageSize" class="input">
            <option :value="5">5</option>
            <option :value="10">10</option>
            <option :value="20">20</option>
          </select>
        </label>
      </div>

      <p v-if="!filteredRecords.length && !store.loading" style="margin-top:1rem;">No records found.</p>

      <div v-if="pagedRecords.length" style="overflow:auto;margin-top:1rem;">
        <table style="width:100%;border-collapse:collapse;">
          <thead>
            <tr>
              <th v-for="field in moduleDef.fields" :key="field.key" style="text-align:left;border-bottom:1px solid #e2e8f0;padding:0.5rem;">{{ field.label }}</th>
              <th v-if="canUpdate || canRemove" style="text-align:right;border-bottom:1px solid #e2e8f0;padding:0.5rem;">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in pagedRecords" :key="item.id">
              <td v-for="field in moduleDef.fields" :key="field.key" style="padding:0.5rem;border-bottom:1px solid #f1f5f9;">
                {{ item[field.key] }}
              </td>
              <td v-if="canUpdate || canRemove" style="padding:0.5rem;border-bottom:1px solid #f1f5f9;text-align:right;">
                <button v-if="canUpdate" class="btn" @click="toEdit(String(item.id))">Edit</button>
                <button v-if="canRemove" class="btn btn-danger" style="margin-left:0.5rem;" @click="openDelete(String(item.id))">Delete</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div style="display:flex;justify-content:flex-end;align-items:center;gap:0.5rem;margin-top:0.75rem;">
        <button class="btn" :disabled="page <= 1" @click="page -= 1">Previous</button>
        <span>Page {{ page }} / {{ totalPages }}</span>
        <button class="btn" :disabled="page >= totalPages" @click="page += 1">Next</button>
      </div>
    </template>
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
