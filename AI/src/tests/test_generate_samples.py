"""
Unit tests for generate_samples.py validation and parsing functions.

Run from AI/src/:
    python -m unittest tests.test_generate_samples -v
"""

import json
import os
import sys
import unittest

# Add the fine-tuning scripts directory to path so we can import without aiohttp
# being required at module level — we import only the pure functions.
SCRIPT_DIR = os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "..", "fine-tuning", "scripts"
)

# We need to mock aiohttp since it may not be installed in the test environment.
# Only the validation/parsing functions are tested here (no async/network code).
import types

if "aiohttp" not in sys.modules:
    sys.modules["aiohttp"] = types.ModuleType("aiohttp")

sys.path.insert(0, SCRIPT_DIR)
from generate_samples import (  # noqa: E402
    NODE_CONFIG,
    parse_generated_lines,
    validate_sample_output,
)


class TestValidateSampleOutput(unittest.TestCase):
    """Tests for validate_sample_output()."""

    # ── Valid outputs ──

    def test_valid_director_output(self):
        output = json.dumps({
            "content": "I'll organize the project.",
            "managers": [
                {"name": "Backend Manager", "job": "Handle API development."},
                {"name": "Frontend Manager", "job": "Build the UI."},
            ],
        })
        ok, msg = validate_sample_output(output, "director")
        self.assertTrue(ok, msg)

    def test_valid_manager_output(self):
        output = json.dumps({
            "content": "Breaking down the backend work.",
            "supervisors": [
                {"name": "DB Supervisor", "job": "Design the database."},
                {"name": "API Supervisor", "job": "Create REST endpoints."},
            ],
        })
        ok, msg = validate_sample_output(output, "manager")
        self.assertTrue(ok, msg)

    def test_valid_supervisor_output(self):
        output = json.dumps({
            "content": "Assigning coding tasks.",
            "agents": [
                {"name": "Auth Agent", "job": "Write login endpoint."},
                {"name": "DB Agent", "job": "Write migration script."},
            ],
        })
        ok, msg = validate_sample_output(output, "supervisor")
        self.assertTrue(ok, msg)

    def test_valid_agent_output(self):
        output = json.dumps({
            "content": "Created the login controller.",
            "files": [
                {"filename": "login.py", "content": "print('hello')"},
                {"filename": "test_login.py", "content": "assert True"},
            ],
        })
        ok, msg = validate_sample_output(output, "agent")
        self.assertTrue(ok, msg)

    # ── Invalid JSON ──

    def test_invalid_json_string(self):
        ok, msg = validate_sample_output("not json at all", "director")
        self.assertFalse(ok)
        self.assertIn("not valid JSON", msg)

    def test_json_array_not_object(self):
        ok, msg = validate_sample_output("[1, 2, 3]", "director")
        self.assertFalse(ok)
        self.assertIn("not a JSON object", msg)

    # ── Missing fields ──

    def test_missing_content_field(self):
        output = json.dumps({
            "managers": [{"name": "M1", "job": "J1"}, {"name": "M2", "job": "J2"}]
        })
        ok, msg = validate_sample_output(output, "director")
        self.assertFalse(ok)
        self.assertIn("content", msg)

    def test_empty_content_field(self):
        output = json.dumps({
            "content": "   ",
            "managers": [{"name": "M1", "job": "J1"}, {"name": "M2", "job": "J2"}],
        })
        ok, msg = validate_sample_output(output, "director")
        self.assertFalse(ok)
        self.assertIn("content", msg)

    def test_missing_array_field(self):
        output = json.dumps({"content": "Plan here."})
        ok, msg = validate_sample_output(output, "director")
        self.assertFalse(ok)
        self.assertIn("managers", msg)

    # ── Array size constraints (2-5 items) ──

    def test_array_too_few_items(self):
        output = json.dumps({
            "content": "Plan.",
            "managers": [{"name": "M1", "job": "J1"}],
        })
        ok, msg = validate_sample_output(output, "director")
        self.assertFalse(ok)
        self.assertIn("at least", msg)

    def test_array_too_many_items(self):
        output = json.dumps({
            "content": "Plan.",
            "managers": [
                {"name": f"M{i}", "job": f"J{i}"} for i in range(6)
            ],
        })
        ok, msg = validate_sample_output(output, "director")
        self.assertFalse(ok)
        self.assertIn("at most", msg)

    def test_array_min_boundary(self):
        output = json.dumps({
            "content": "Plan.",
            "managers": [
                {"name": "M1", "job": "J1"},
                {"name": "M2", "job": "J2"},
            ],
        })
        ok, msg = validate_sample_output(output, "director")
        self.assertTrue(ok, msg)

    def test_array_max_boundary(self):
        output = json.dumps({
            "content": "Plan.",
            "managers": [
                {"name": f"M{i}", "job": f"J{i}"} for i in range(5)
            ],
        })
        ok, msg = validate_sample_output(output, "director")
        self.assertTrue(ok, msg)

    # ── Item validation ──

    def test_item_missing_required_field(self):
        output = json.dumps({
            "content": "Plan.",
            "managers": [
                {"name": "M1"},
                {"name": "M2", "job": "J2"},
            ],
        })
        ok, msg = validate_sample_output(output, "director")
        self.assertFalse(ok)
        self.assertIn("job", msg)

    def test_item_empty_field(self):
        output = json.dumps({
            "content": "Plan.",
            "managers": [
                {"name": "", "job": "J1"},
                {"name": "M2", "job": "J2"},
            ],
        })
        ok, msg = validate_sample_output(output, "director")
        self.assertFalse(ok)
        self.assertIn("empty", msg)

    def test_item_not_object(self):
        output = json.dumps({
            "content": "Plan.",
            "managers": ["not an object", {"name": "M2", "job": "J2"}],
        })
        ok, msg = validate_sample_output(output, "director")
        self.assertFalse(ok)
        self.assertIn("not an object", msg)

    # ── Agent-specific (filename/content) ──

    def test_agent_item_missing_filename(self):
        output = json.dumps({
            "content": "Done.",
            "files": [
                {"content": "code"},
                {"filename": "b.py", "content": "code"},
            ],
        })
        ok, msg = validate_sample_output(output, "agent")
        self.assertFalse(ok)
        self.assertIn("filename", msg)


class TestParseGeneratedLines(unittest.TestCase):
    """Tests for parse_generated_lines()."""

    def _make_line(self, instruction="test", inp="test input", output_obj=None):
        if output_obj is None:
            output_obj = {
                "content": "Plan.",
                "managers": [
                    {"name": "M1", "job": "J1"},
                    {"name": "M2", "job": "J2"},
                ],
            }
        return json.dumps({
            "instruction": instruction,
            "input": inp,
            "output": json.dumps(output_obj),
        })

    def test_valid_line_parsed(self):
        line = self._make_line()
        valid, invalid = parse_generated_lines(line, "director")
        self.assertEqual(len(valid), 1)
        self.assertEqual(invalid, 0)

    def test_instruction_normalised(self):
        line = self._make_line(instruction="some other instruction")
        valid, _ = parse_generated_lines(line, "director")
        self.assertEqual(valid[0]["instruction"], NODE_CONFIG["director"]["instruction"])

    def test_invalid_json_line_skipped(self):
        raw = "not json\n" + self._make_line()
        valid, invalid = parse_generated_lines(raw, "director")
        self.assertEqual(len(valid), 1)
        self.assertEqual(invalid, 1)

    def test_missing_required_keys_skipped(self):
        raw = json.dumps({"instruction": "test", "input": "test"})  # no output
        valid, invalid = parse_generated_lines(raw, "director")
        self.assertEqual(len(valid), 0)
        self.assertEqual(invalid, 1)

    def test_output_as_object_auto_converted(self):
        """When model returns output as object instead of string, it should be converted."""
        output_obj = {
            "content": "Plan.",
            "managers": [
                {"name": "M1", "job": "J1"},
                {"name": "M2", "job": "J2"},
            ],
        }
        line = json.dumps({
            "instruction": "test",
            "input": "test input",
            "output": output_obj,  # object, not string
        })
        valid, invalid = parse_generated_lines(line, "director")
        self.assertEqual(len(valid), 1)
        self.assertIsInstance(valid[0]["output"], str)

    def test_markdown_fences_stripped(self):
        raw = "```json\n" + self._make_line() + "\n```"
        valid, invalid = parse_generated_lines(raw, "director")
        self.assertEqual(len(valid), 1)

    def test_empty_lines_skipped(self):
        raw = "\n\n" + self._make_line() + "\n\n"
        valid, invalid = parse_generated_lines(raw, "director")
        self.assertEqual(len(valid), 1)
        self.assertEqual(invalid, 0)

    def test_empty_input_skipped(self):
        line = json.dumps({
            "instruction": "test",
            "input": "",
            "output": json.dumps({
                "content": "Plan.",
                "managers": [
                    {"name": "M1", "job": "J1"},
                    {"name": "M2", "job": "J2"},
                ],
            }),
        })
        valid, invalid = parse_generated_lines(line, "director")
        self.assertEqual(len(valid), 0)
        self.assertEqual(invalid, 1)

    def test_multiple_valid_lines(self):
        lines = "\n".join([
            self._make_line(inp=f"project {i}") for i in range(3)
        ])
        valid, invalid = parse_generated_lines(lines, "director")
        self.assertEqual(len(valid), 3)
        self.assertEqual(invalid, 0)


if __name__ == "__main__":
    unittest.main()
