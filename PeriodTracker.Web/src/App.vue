<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { CalendarDays, Droplets, RefreshCw, Save } from "lucide-vue-next";

// const userId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
// const userId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
// const userId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const userId = "dddddddd-dddd-dddd-dddd-dddddddddddd";
// const userId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";

type PeriodLog = {
  id: string;
  startDate: string;
  endDate: string;
  length: number;
  flow: string;
  notes: string;
};

type Prediction = {
  status: string;
  basis: string;
  confidence: number;
  predictionType: string;
  averageCycleLength: number;
  averagePeriodDuration: number;
  predictedNextPeriod: string;
  ovulationDate: string;
  follicularPhaseLength: number;
  fertileWindow: { startDate: string; endDate: string };
  cycleVariation: number;
  isIrregularCycle: boolean;
  daysSinceLastPeriod: number | null;
  confidenceScore: number;
  latestPeriodStart: string | null;
  predictedCycleLength: number | null;
  predictedPeriodLength: number | null;
  nextPeriod: { startDate: string; endDate: string } | null;
  ovulation: {
    date: string;
    fertileWindowStart: string;
    fertileWindowEnd: string;
  } | null;
  source: string;
  disclaimer: string;
};

type PeriodForm = {
  startDate: string;
  endDate: string;
  flow: string;
  notes: string;
};

const emptyForm = (): PeriodForm => ({
  startDate: "",
  endDate: "",
  flow: "medium",
  notes: ""
});

const periods = ref<PeriodLog[]>([]);
const prediction = ref<Prediction | null>(null);
const form = reactive<PeriodForm>(emptyForm());
const message = ref("");
const loading = ref(true);

const latestPeriod = computed(() => periods.value[0]);

async function refresh() {
  loading.value = true;
  const [periodResponse, predictionResponse] = await Promise.all([
    fetch(`/api/periods/get-period-history/${userId}`),
    fetch(`/api/predictions/get-prediction/${userId}`)
  ]);

  periods.value = await periodResponse.json();
  prediction.value = await predictionResponse.json();
  loading.value = false;
}

async function submitPeriod() {
  message.value = "";

  const response = await fetch(`/api/periods/add-period-entry/${userId}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(form)
  });

  if (!response.ok) {
    const body = await response.json();
    message.value = body.error ?? "Could not save this period.";
    return;
  }

  Object.assign(form, emptyForm());
  message.value = "Saved. Prediction refreshed from the latest logs.";
  await refresh();
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en", {
    month: "short",
    day: "numeric",
    year: "numeric"
  }).format(new Date(`${value}T00:00:00`));
}

function formatPredictionType(value: string) {
  return value
    .split("_")
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

onMounted(() => {
  refresh().catch(() => {
    message.value = "Could not reach the API. Start the .NET backend on port 5090.";
    loading.value = false;
  });
});
</script>

<template>
  <main class="app">
    <section class="masthead">
      <div>
        <p class="eyebrow">Cycle insights</p>
        <h1>Period Tracker</h1>
        <p class="lede">
          Log periods, estimate the next cycle, and view an ovulation window
          from recent history.
        </p>
      </div>

      <aside v-if="loading" class="prediction-card">Loading prediction...</aside>
      <aside v-else-if="!prediction" class="prediction-card">Add a period log to unlock predictions.</aside>
      <aside v-else class="prediction-card">
        <p class="card-label">Next period</p>
        <h2>{{ formatDate(prediction.predictedNextPeriod) }}</h2>
        <dl>
          <div>
            <dt>Cycle length</dt>
            <dd>{{ prediction.averageCycleLength }} days</dd>
          </div>
          <div>
            <dt>Period duration</dt>
            <dd>{{ prediction.averagePeriodDuration }} days</dd>
          </div>
          <div>
            <dt>Ovulation</dt>
            <dd>{{ formatDate(prediction.ovulationDate) }}</dd>
          </div>
          <div>
            <dt>Fertile window</dt>
            <dd>
              {{ formatDate(prediction.fertileWindow.startDate) }} -
              {{ formatDate(prediction.fertileWindow.endDate) }}
            </dd>
          </div>
          <div>
            <dt>Follicular phase</dt>
            <dd>{{ prediction.follicularPhaseLength }} days</dd>
          </div>
          <div>
            <dt>Variation</dt>
            <dd>{{ prediction.cycleVariation }} days</dd>
          </div>
          <div>
            <dt>Irregular</dt>
            <dd>{{ prediction.isIrregularCycle ? "Yes" : "No" }}</dd>
          </div>
          <div v-if="prediction.daysSinceLastPeriod !== null">
            <dt>Last period</dt>
            <dd>{{ prediction.daysSinceLastPeriod }} days ago</dd>
          </div>
          <div>
            <dt>Type</dt>
            <dd>
              {{ formatPredictionType(prediction.predictionType) }}
            </dd>
          </div>
          <div>
            <dt>Confidence</dt>
            <dd>{{ Math.round(prediction.confidenceScore * 100) }}%</dd>
          </div>
        </dl>
        <p class="basis">{{ prediction.basis }}</p>
      </aside>
    </section>

    <section class="content-grid">
      <form class="panel form-panel" @submit.prevent="submitPeriod">
        <div class="section-title">
          <Droplets :size="20" aria-hidden="true" />
          <h2>Log Period</h2>
        </div>

        <label>
          Start date
          <input v-model="form.startDate" type="date" required />
        </label>

        <label>
          End date
          <input v-model="form.endDate" type="date" required />
        </label>

        <label>
          Flow
          <select v-model="form.flow">
            <option value="light">Light</option>
            <option value="medium">Medium</option>
            <option value="heavy">Heavy</option>
          </select>
        </label>

        <label>
          Notes
          <textarea
            v-model="form.notes"
            rows="3"
            placeholder="Symptoms, mood, medication"
          />
        </label>

        <button type="submit">
          <Save :size="18" aria-hidden="true" />
          Save period
        </button>
        <p v-if="message" class="message">{{ message }}</p>
      </form>

      <section class="panel">
        <div class="section-title split">
          <span>
            <CalendarDays :size="20" aria-hidden="true" />
            <h2>Recent Logs</h2>
          </span>
          <button class="icon-button" type="button" aria-label="Refresh" @click="refresh">
            <RefreshCw :size="18" aria-hidden="true" />
          </button>
        </div>

        <p v-if="latestPeriod" class="latest">
          Latest period started
          <strong>{{ formatDate(latestPeriod.startDate) }}</strong>.
        </p>

        <div class="log-list">
          <article v-for="period in periods" :key="period.id" class="log-item">
            <div>
              <strong>
                {{ formatDate(period.startDate) }} - {{ formatDate(period.endDate) }}
              </strong>
              <span>{{ period.length }} days</span>
            </div>
            <div class="flow">{{ period.flow }}</div>
            <p v-if="period.notes">{{ period.notes }}</p>
          </article>
        </div>
      </section>
    </section>
  </main>
</template>
