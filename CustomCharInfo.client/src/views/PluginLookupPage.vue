<template>
  <PageShell title="Plugin lookup">
    <template #subnav>
      <SubNav section="movesets" label="Movesets" />
    </template>

    <p class="lookup-intro">
      Not sure what a <code>.nro</code> is or whether it's up to date? Upload the file below to hash
      it entirely in the browser and check it against UMC's database. You can also drop several
      files or your whole <code>plugins</code> folder to check everything at once. Have a plugin
      that is not in the database yet? Go to the
      <router-link to="/plugins/add">submission page</router-link>.
    </p>

    <div class="picker-row">
      <PluginBatchDropZone v-model="entries" large />
    </div>

    <AppLoading v-if="loading" :label="loadingLabel" />

    <div v-else-if="error" class="result result--err">
      <v-icon class="result__icon">mdi-alert-circle</v-icon>
      <span>{{ error }}</span>
    </div>

    <!-- One file: the single result card -->
    <template v-else-if="rows && rows.length === 1">
      <div v-if="!singleResult" class="result result--neutral">
        <v-icon class="result__icon">mdi-help-circle</v-icon>
        <div>
          <div class="result__title">Not recognized</div>
          <div class="result__sub">
            This hash doesn't match any plugin in the database. If you know what it is, please
            <router-link to="/plugins/add">submit it</router-link>.
          </div>
        </div>
      </div>

      <div v-else class="result" :class="singleResult.isCurrent ? 'result--ok' : 'result--warn'">
        <v-icon class="result__icon">{{
          singleResult.isCurrent ? 'mdi-check-circle' : 'mdi-alert'
        }}</v-icon>
        <div class="result__body">
          <div class="result__title">
            <router-link
              v-if="singleResult.attachmentType === 'Moveset'"
              :to="{ name: 'MovesetDetail', params: { movesetId: singleResult.movesetId } }"
              >{{ singleResult.pluginName }}</router-link
            >
            <template v-else>{{ singleResult.pluginName }}</template>
          </div>
          <div class="result__sub">Version {{ singleResult.matchedVersionLabel }}</div>
          <p
            v-if="singleResult.attachmentType === 'Other' && singleResult.pluginDescription"
            class="result__desc"
          >
            {{ singleResult.pluginDescription }}
          </p>

          <div class="result__version">
            <span v-if="singleResult.isCurrent">Up to date. This is the most recent version.</span>
            <span v-else>
              There is a
              <a
                v-if="singleResult.learnMoreUrl"
                :href="singleResult.learnMoreUrl"
                target="_blank"
                rel="noopener"
                >newer version of this {{ thingLabel(singleResult) }}</a
              ><template v-else>newer version of this {{ thingLabel(singleResult) }}</template>
              available<template v-if="singleResult.currentVersionLabel">
                ({{ displayVersion(singleResult.currentVersionLabel) }})</template
              >.
            </span>
          </div>

          <!--
            Moveset plugins link the title itself to the moveset's page on UMC;
            dependency/other plugins have no such page,
            so their link needs to stay visible here even when current (when outdated, the sentence above already links to it).
          -->
          <a
            v-if="
              singleResult.attachmentType !== 'Moveset' &&
              singleResult.isCurrent &&
              singleResult.learnMoreUrl
            "
            :href="singleResult.learnMoreUrl"
            target="_blank"
            rel="noopener"
            class="learn-more-link"
          >
            View mod page <v-icon size="small">mdi-open-in-new</v-icon>
          </a>
        </div>
      </div>
    </template>

    <!-- Several files: summary line and table -->
    <template v-else-if="rows && rows.length > 1">
      <div class="batch-summary">
        <StatusTag variant="ok" icon="mdi-check-circle">{{ summary.current }} up to date</StatusTag>
        <StatusTag variant="warn" icon="mdi-alert">{{ summary.outdated }} outdated</StatusTag>
        <StatusTag variant="outline" icon="mdi-help-circle"
          >{{ summary.unknown }} not recognized</StatusTag
        >
      </div>

      <TableScroll min-width="640px">
        <table class="batch-table">
          <thead>
            <tr>
              <th>File</th>
              <th>Plugin</th>
              <th>Version</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="row in rows" :key="row.path + row.hash">
              <td class="cell-file mono" :title="row.path">{{ row.path }}</td>

              <td v-if="row.found">
                <router-link
                  v-if="row.result.attachmentType === 'Moveset'"
                  :to="{ name: 'MovesetDetail', params: { movesetId: row.result.movesetId } }"
                  >{{ row.result.pluginName }}</router-link
                >
                <template v-else>{{ row.result.pluginName }}</template>
              </td>
              <td v-else class="cell-muted">
                Unknown.
                <router-link to="/plugins/add">Submit it?</router-link>
              </td>

              <td>
                <template v-if="row.found">{{ row.result.matchedVersionLabel }}</template>
              </td>

              <td>
                <StatusTag :variant="STATUS_TONES[rowStatus(row)]">
                  <template v-if="rowStatus(row) === 'current'">Up to date</template>
                  <template v-else-if="rowStatus(row) === 'outdated'">
                    Newer available<template v-if="row.result.currentVersionLabel">
                      ({{ displayVersion(row.result.currentVersionLabel) }})</template
                    >
                  </template>
                  <template v-else>Not recognized</template>
                </StatusTag>
              </td>

              <td>
                <a
                  v-if="row.found && row.result.learnMoreUrl"
                  :href="row.result.learnMoreUrl"
                  target="_blank"
                  rel="noopener"
                  class="learn-more-link learn-more-link--inline"
                  title="View mod page"
                >
                  <v-icon size="small">mdi-open-in-new</v-icon>
                </a>
              </td>
            </tr>
          </tbody>
        </table>
      </TableScroll>
    </template>
  </PageShell>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import api from '@/services/api'
import PageShell from '@/components/PageShell.vue'
import SubNav from '@/components/SubNav.vue'
import AppLoading from '@/components/AppLoading.vue'
import StatusTag from '@/components/StatusTag.vue'
import TableScroll from '@/components/TableScroll.vue'
import PluginBatchDropZone from '@/components/PluginBatchDropZone.vue'
import { hashFile } from '@/services/hashFile'
import { displayVersion } from '@/services/pluginVersion'
import {
  chunk,
  groupBatchResults,
  rowStatus,
  summarizeRows,
  BATCH_IDENTIFY_MAX_HASHES,
} from '@/services/pluginBatch'

const MAX_FILES = 200
const STATUS_TONES = { current: 'ok', outdated: 'warn', unknown: 'outline' }

const entries = ref([])
const loading = ref(false)
const loadingLabel = ref('')
const error = ref(null)
const rows = ref(null)

const singleResult = computed(() => (rows.value?.[0]?.found ? rows.value[0].result : null))
const summary = computed(() => summarizeRows(rows.value ?? []))

const thingLabel = (result) => (result?.attachmentType === 'Dependency' ? 'dependency' : 'mod')

async function runLookup(list) {
  error.value = null
  rows.value = null
  if (!list || list.length === 0) return

  if (list.length > MAX_FILES) {
    error.value = `That's ${list.length} files. Please check at most ${MAX_FILES} at a time.`
    return
  }

  loading.value = true
  loadingLabel.value =
    list.length === 1 ? 'Hashing and checking' : `Hashing ${list.length} files and checking`
  try {
    const hashes = await Promise.all(list.map((entry) => hashFile(entry.file)))
    const unique = [...new Set(hashes)]
    const responses = await Promise.all(
      chunk(unique, BATCH_IDENTIFY_MAX_HASHES).map((hashesChunk) =>
        api.post('/plugins/identify/batch', { hashes: hashesChunk })
      )
    )
    const results = responses.flatMap((res) => res.data)

    if (entries.value !== list) return // a newer selection was made while this was in flight
    rows.value = groupBatchResults(list, hashes, results)
  } catch {
    if (entries.value !== list) return
    error.value = 'Something went wrong while checking these files. Please try again.'
  } finally {
    if (entries.value === list) loading.value = false
  }
}

watch(entries, runLookup)
</script>

<style scoped>
.lookup-intro {
  max-width: 720px;
  margin: 0 0 20px;
  color: var(--tx-2);
  line-height: 1.55;
}

.lookup-intro a,
.result a,
.batch-table a {
  color: var(--white);
  text-decoration: underline;
}

.picker-row {
  margin-bottom: 20px;
}

/* One result */
.result {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 16px 20px;
  border: 1px solid var(--line-2);
  border-left-width: 4px;
  background: var(--panel);
  color: var(--tx);
}

.result--ok {
  border-left-color: var(--ok);
}

.result--ok .result__icon {
  color: var(--ok);
}

.result--warn {
  border-left-color: var(--warn);
}

.result--warn .result__icon {
  color: var(--warn);
}

.result--err {
  border-left-color: var(--err);
}

.result--err .result__icon {
  color: var(--err);
}

.result--neutral {
  border-left-color: var(--tx-3);
}

.result__icon {
  font-size: 1.5em;
  flex-shrink: 0;
  margin-top: 1px;
  color: var(--tx-2);
}

.result__title {
  font-size: 1.15em;
  font-weight: 600;
}

.result__sub {
  font-size: 0.85em;
  color: var(--tx-2);
  margin-top: 2px;
}

.result__desc {
  margin: 8px 0 0;
  font-size: 0.92em;
  line-height: 1.4;
}

.result__version {
  margin-top: 10px;
  font-size: 0.92em;
}

.learn-more-link {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  margin-top: 10px;
  font-size: 0.88em;
}

.learn-more-link--inline {
  margin-top: 0;
}

/* Batch */
.batch-summary {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 14px;
}

.batch-table {
  width: 100%;
  border-collapse: collapse;
  border: 1px solid var(--line);
  background: var(--panel);
  font-size: 14px;
}

.batch-table th,
.batch-table td {
  padding: 8px 12px;
  text-align: left;
  border-bottom: 1px solid var(--line);
  vertical-align: middle;
  white-space: nowrap;
}

.batch-table th {
  color: var(--tx-2);
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.03em;
  border-bottom-color: var(--white);
}

.batch-table tbody tr:hover td {
  background: var(--panel-2);
}

.batch-table tbody tr:last-child td {
  border-bottom: 0;
}

.cell-file {
  max-width: 320px;
  overflow: hidden;
  text-overflow: ellipsis;
}

.cell-muted {
  color: var(--tx-3);
}
</style>
