"use client";

import { StageQueueClient } from "@/components/StageQueueClient";
import { getSewingQueue } from "@/lib/api/sewing";

export function SewingClient() {
  return (
    <StageQueueClient
      stage="sewing"
      requireUserForQueue
      emptyHint="Nenhuma costura atribuída a esta costureira no momento."
      queueLoader={(userId) =>
        userId ? getSewingQueue(userId) : Promise.resolve([])
      }
    />
  );
}
