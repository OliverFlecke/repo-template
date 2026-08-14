"""Minimal NVFlare job: each round, every client increments a shared counter by 1
and the server sums the results. No ML, just enough to prove server/client wiring works.
"""

import time
from typing import override

from nvflare.apis.controller_spec import ClientTask, Task
from nvflare.apis.executor import Executor
from nvflare.apis.fl_constant import ReturnCode
from nvflare.apis.fl_context import FLContext
from nvflare.apis.impl.controller import Controller
from nvflare.apis.shareable import Shareable, make_reply
from nvflare.apis.signal import Signal

TASK_NAME = "count"


class CounterController(Controller):
    rounds: int
    timeout: int
    total: int

    def __init__(self, rounds: int = 5, timeout: int = 30):
        """
        Args:
            rounds: number of broadcast rounds to run.
            timeout: seconds to wait for a round's stragglers before moving
                on without them (0 = wait forever).
        """
        super().__init__()
        self.rounds = rounds
        self.timeout = timeout
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
                name=TASK_NAME,
                data=shareable,
                timeout=self.timeout,
                result_received_cb=self._on_result,
            )
            self.broadcast_and_wait(
                task=task,
                fl_ctx=fl_ctx,
                min_responses=0,  # wait for every client
                abort_signal=abort_signal,
            )

            missing = [
                ct.client.name
                for ct in task.client_tasks  # pyright: ignore[reportUnknownMemberType]
                if ct.result_received_time is None
            ]
            responded = len(task.client_tasks) - len(missing)
            self.log_info(
                fl_ctx,
                f"round {round_num} done, total={self.total}, "
                f"{responded}/{len(task.client_tasks)} clients responded"
                + (f", missing: {missing}" if missing else ""),
            )
            time.sleep(1)

        self.log_warning(fl_ctx, f"finished, final total={self.total}")


class CounterExecutor(Executor):
    @override
    def execute(
        self,
        task_name: str,
        shareable: Shareable,
        fl_ctx: FLContext,
        abort_signal: Signal,
    ) -> Shareable:
        if task_name != TASK_NAME:
            return make_reply(ReturnCode.TASK_UNSUPPORTED)

        server_total = shareable["total"]
        self.log_warning(fl_ctx, f"saw server total={server_total}, incrementing by 1")

        result = Shareable()
        result["count"] = 1

        try:
            import os

            with open(os.path.join("/workspace/data", "sample.csv"), "r") as f:
                data = f.read()
                self.log_warning(fl_ctx, f"found {data}")
        except OSError as e:
            self.log_warning(fl_ctx, f"unable to find data: {e}")

        return result


def main() -> None:
    """Build the counter job and export it to ./jobs, ready for `nvflare job submit`."""
    from nvflare.job_config.api import FedJob

    job = FedJob(name="counter", min_clients=3)
    job.to_server(CounterController(rounds=1, timeout=30))
    job.to_clients(CounterExecutor())
    job.export_job("jobs")


if __name__ == "__main__":
    main()