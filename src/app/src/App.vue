<script setup lang="ts">
import PublishChangesDialog from '@/components/publishing/PublishChangesDialog.vue'
import PublishTogetherDialog from '@/components/publishing/PublishTogetherDialog.vue'
import ScheduleDialog from '@/components/publishing/ScheduleDialog.vue'
</script>

<template>
  <RouterView />

  <!-- Publishing dialogs, opened from any editor and the top bar (usePublishDialog). -->
  <PublishChangesDialog />
  <PublishTogetherDialog />
  <ScheduleDialog />
</template>
