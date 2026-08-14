"""Minimal NVFlare job: each round, every client increments a shared counter by 1
and the server sums the results. No ML, just enough to prove server/client wiring works.
"""

import time
from typing import override

from nvflare.apis.controller_spec import ClientTask, Task
from nvflare.apis.executor import Executor
from nvflare.apis.fl_context import FLContext
from nvflare.apis.impl.controller import Controller
from nvflare.apis.shareable import Shareable
from nvflare.apis.signal import Signal

TASK_NAME = "count"


class CounterController(Controller):
    rounds: int
    min_clients: int
    total: int

    def __init__(self, rounds: int = 5, min_clients: int = 1):
        super().__init__()
        self.rounds = rounds
        self.min_clients = min_clients
        self.total = 0

    @override
    def start_controller(self, fl_ctx: FLContext):
        self.total = 0

    @override
    def stop_controller(self, fl_ctx: FLContext):
        pass

    def _on_result(self, client_task: ClientTask, fl_ctx: FLContext):
        self.total += client_task.result["count"]

        self.log_info(
            fl_ctx,
            f"{client_task.client.name} reported count={client_task.result['count']}",
        )

    @override
    def control_flow(self, abort_signal: Signal, fl_ctx: FLContext):
        for round_num in range(self.rounds):
            if abort_signal.triggered:
                return

            shareable = Shareable()
            shareable["total"] = self.total
            task = Task(
                name=TASK_NAME, data=shareable, result_received_cb=self._on_result
            )
            self.broadcast_and_wait(
                task=task,
                fl_ctx=fl_ctx,
                min_responses=self.min_clients,
                abort_signal=abort_signal,
            )
            self.log_info(fl_ctx, f"round {round_num} done, total={self.total}")
            time.sleep(1)

        self.log_info(fl_ctx, f"finished, final total={self.total}")


class CounterExecutor(Executor):
    def execute(
        self,
        task_name: str,
        shareable: Shareable,
        fl_ctx: FLContext,
        abort_signal: Signal,
    ) -> Shareable:
        if task_name != TASK_NAME:
            return Shareable()

        server_total = shareable["total"]
        self.log_info(fl_ctx, f"saw server total={server_total}, incrementing by 1")

        result = Shareable()
        result["count"] = 1
        return result


def main() -> None:
    """Build the counter job and export it to ./jobs, ready for `nvflare job submit`."""
    from nvflare.job_config.api import FedJob

    job = FedJob(name="counter", min_clients=3)
    job.to_server(CounterController(rounds=5, min_clients=3))
    job.to_clients(CounterExecutor())
    job.export_job("jobs")


if __name__ == "__main__":
    main()