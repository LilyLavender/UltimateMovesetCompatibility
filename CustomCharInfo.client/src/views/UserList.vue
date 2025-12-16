<template>
  <v-container max-width="1020px">
    <!-- Page title -->
    <h1 class="mb-5 page-title no-select">All Users</h1>

    <!-- Full user -->
    <section class="mb-8">
      <h2>Full users</h2>
      <v-data-table
        class="dark-table"
        :items="inBoth"
        :headers="bothHeaders"
        item-key="user.id"
        density="comfortable"
      />
    </section>

    <!-- Only ApplicationUser -->
    <section class="mb-8">
      <h2>Users without Modder profile</h2>
      <v-data-table
        class="dark-table"
        :items="onlyUsers"
        :headers="userHeaders"
        item-key="id"
      />
    </section>

    <!-- Only Modders -->
    <section class="mb-8">
      <h2>Modders without user account</h2>
      <v-data-table
        class="dark-table"
        :items="onlyModders"
        :headers="modderHeaders"
        item-key="modderId"
      />
    </section>
  </v-container>
</template>

<script setup>
import { ref, onMounted } from "vue";
import api from "@/services/api";

const onlyUsers = ref([]);
const onlyModders = ref([]);
const inBoth = ref([]);

// Table headers
const userHeaders = [
  { title: "ID", key: "id" },
  { title: "UserName", key: "userName" },
  { title: "Email", key: "email" },
  { title: "UserType", key: "userTypeId" },
  { title: "ModderId", key: "modderId" },
];

const modderHeaders = [
  { title: "ModderId", key: "modderId" },
  { title: "Name", key: "name" },
  { title: "Bio", key: "bio" },
  { title: "GamebananaId", key: "gamebananaId" },
  { title: "UserId", key: "userId" },
  { title: "DiscordUsername", key: "discordUsername" },
];

const bothHeaders = [
  { title: "User ID", key: "user.id" },
  { title: "UserName", key: "user.userName" },
  { title: "Email", key: "user.email" },
  { title: "UserType", key: "user.userTypeId" },
  { title: "ModderId", key: "user.modderId" },
  { title: "Modder Name", key: "modder.name" },
  { title: "Bio", key: "modder.bio" },
  { title: "GamebananaId", key: "modder.gamebananaId" },
  { title: "DiscordUsername", key: "modder.discordUsername" },
];

// Fetch all users on mount
onMounted(async () => {
  try {
    const res = await api.get("/users");
    onlyUsers.value = res.data.onlyUsers;
    onlyModders.value = res.data.onlyModders;
    inBoth.value = res.data.inBoth;
  } catch (err) {
    console.error("Failed to fetch users:", err);
  }
});
</script>

<style scoped>
.page-title {
  font-size: 2.5rem;
}
section {
  background-color: #1e1e1e;
  padding: 1rem;
  border-radius: 10px;
}
</style>
