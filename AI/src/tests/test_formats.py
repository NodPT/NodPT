import json
import os
import unittest

BASE_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
NODE_TYPES = ["Director", "Manager", "Supervisor", "Agent"]


def load_format(node_type):
    path = os.path.join(BASE_DIR, node_type, "format.json")
    with open(path, "r") as f:
        return json.load(f)


class TestDirectorFormat(unittest.TestCase):
    def setUp(self):
        self.fmt = load_format("Director")

    def test_is_object_type(self):
        self.assertEqual(self.fmt["type"], "object")

    def test_has_content_property(self):
        self.assertIn("content", self.fmt["properties"])
        self.assertEqual(self.fmt["properties"]["content"]["type"], "string")

    def test_has_managers_array(self):
        self.assertIn("managers", self.fmt["properties"])
        self.assertEqual(self.fmt["properties"]["managers"]["type"], "array")

    def test_manager_items_have_name_and_job(self):
        items = self.fmt["properties"]["managers"]["items"]
        self.assertEqual(items["type"], "object")
        self.assertIn("name", items["properties"])
        self.assertIn("job", items["properties"])
        self.assertEqual(items["properties"]["name"]["type"], "string")
        self.assertEqual(items["properties"]["job"]["type"], "string")

    def test_required_fields(self):
        self.assertIn("content", self.fmt["required"])
        self.assertIn("managers", self.fmt["required"])

    def test_manager_items_required_fields(self):
        items = self.fmt["properties"]["managers"]["items"]
        self.assertIn("name", items["required"])
        self.assertIn("job", items["required"])


class TestManagerFormat(unittest.TestCase):
    def setUp(self):
        self.fmt = load_format("Manager")

    def test_is_object_type(self):
        self.assertEqual(self.fmt["type"], "object")

    def test_has_content_property(self):
        self.assertIn("content", self.fmt["properties"])
        self.assertEqual(self.fmt["properties"]["content"]["type"], "string")

    def test_has_supervisors_array(self):
        self.assertIn("supervisors", self.fmt["properties"])
        self.assertEqual(self.fmt["properties"]["supervisors"]["type"], "array")

    def test_supervisor_items_have_name_and_job(self):
        items = self.fmt["properties"]["supervisors"]["items"]
        self.assertEqual(items["type"], "object")
        self.assertIn("name", items["properties"])
        self.assertIn("job", items["properties"])
        self.assertEqual(items["properties"]["name"]["type"], "string")
        self.assertEqual(items["properties"]["job"]["type"], "string")

    def test_required_fields(self):
        self.assertIn("content", self.fmt["required"])
        self.assertIn("supervisors", self.fmt["required"])

    def test_supervisor_items_required_fields(self):
        items = self.fmt["properties"]["supervisors"]["items"]
        self.assertIn("name", items["required"])
        self.assertIn("job", items["required"])


class TestSupervisorFormat(unittest.TestCase):
    def setUp(self):
        self.fmt = load_format("Supervisor")

    def test_is_object_type(self):
        self.assertEqual(self.fmt["type"], "object")

    def test_has_content_property(self):
        self.assertIn("content", self.fmt["properties"])
        self.assertEqual(self.fmt["properties"]["content"]["type"], "string")

    def test_has_agents_array(self):
        self.assertIn("agents", self.fmt["properties"])
        self.assertEqual(self.fmt["properties"]["agents"]["type"], "array")

    def test_agent_items_have_name_and_job(self):
        items = self.fmt["properties"]["agents"]["items"]
        self.assertEqual(items["type"], "object")
        self.assertIn("name", items["properties"])
        self.assertIn("job", items["properties"])
        self.assertEqual(items["properties"]["name"]["type"], "string")
        self.assertEqual(items["properties"]["job"]["type"], "string")

    def test_required_fields(self):
        self.assertIn("content", self.fmt["required"])
        self.assertIn("agents", self.fmt["required"])

    def test_agent_items_required_fields(self):
        items = self.fmt["properties"]["agents"]["items"]
        self.assertIn("name", items["required"])
        self.assertIn("job", items["required"])


class TestAgentFormat(unittest.TestCase):
    def setUp(self):
        self.fmt = load_format("Agent")

    def test_is_object_type(self):
        self.assertEqual(self.fmt["type"], "object")

    def test_has_content_property(self):
        self.assertIn("content", self.fmt["properties"])
        self.assertEqual(self.fmt["properties"]["content"]["type"], "string")

    def test_has_files_array(self):
        self.assertIn("files", self.fmt["properties"])
        self.assertEqual(self.fmt["properties"]["files"]["type"], "array")

    def test_file_items_have_filename_and_content(self):
        items = self.fmt["properties"]["files"]["items"]
        self.assertEqual(items["type"], "object")
        self.assertIn("filename", items["properties"])
        self.assertIn("content", items["properties"])
        self.assertEqual(items["properties"]["filename"]["type"], "string")
        self.assertEqual(items["properties"]["content"]["type"], "string")

    def test_required_fields(self):
        self.assertIn("content", self.fmt["required"])
        self.assertIn("files", self.fmt["required"])

    def test_file_items_required_fields(self):
        items = self.fmt["properties"]["files"]["items"]
        self.assertIn("filename", items["required"])
        self.assertIn("content", items["required"])


class TestNodeHierarchy(unittest.TestCase):
    def test_all_formats_are_objects(self):
        for node_type in NODE_TYPES:
            fmt = load_format(node_type)
            self.assertEqual(fmt["type"], "object", f"{node_type} should be object type")

    def test_all_formats_have_content(self):
        for node_type in NODE_TYPES:
            fmt = load_format(node_type)
            self.assertIn("content", fmt["properties"], f"{node_type} missing content")
            self.assertIn("content", fmt["required"], f"{node_type} content not required")

    def test_director_only_has_managers(self):
        fmt = load_format("Director")
        self.assertIn("managers", fmt["properties"])
        self.assertNotIn("supervisors", fmt["properties"])
        self.assertNotIn("agents", fmt["properties"])
        self.assertNotIn("files", fmt["properties"])

    def test_manager_only_has_supervisors(self):
        fmt = load_format("Manager")
        self.assertNotIn("managers", fmt["properties"])
        self.assertIn("supervisors", fmt["properties"])
        self.assertNotIn("agents", fmt["properties"])
        self.assertNotIn("files", fmt["properties"])

    def test_supervisor_only_has_agents(self):
        fmt = load_format("Supervisor")
        self.assertNotIn("managers", fmt["properties"])
        self.assertNotIn("supervisors", fmt["properties"])
        self.assertIn("agents", fmt["properties"])
        self.assertNotIn("files", fmt["properties"])

    def test_agent_only_has_files(self):
        fmt = load_format("Agent")
        self.assertNotIn("managers", fmt["properties"])
        self.assertNotIn("supervisors", fmt["properties"])
        self.assertNotIn("agents", fmt["properties"])
        self.assertIn("files", fmt["properties"])


class TestPrompts(unittest.TestCase):
    def test_each_node_type_has_prompts_folder(self):
        for node_type in NODE_TYPES:
            prompts_dir = os.path.join(BASE_DIR, node_type, "prompts")
            self.assertTrue(
                os.path.isdir(prompts_dir), f"{node_type}/prompts/ should exist"
            )

    def test_each_node_type_has_sample_prompt(self):
        for node_type in NODE_TYPES:
            path = os.path.join(BASE_DIR, node_type, "prompts", "sample.txt")
            self.assertTrue(
                os.path.isfile(path), f"{node_type}/prompts/sample.txt should exist"
            )

    def test_sample_prompts_are_non_empty(self):
        for node_type in NODE_TYPES:
            path = os.path.join(BASE_DIR, node_type, "prompts", "sample.txt")
            with open(path, "r") as f:
                content = f.read().strip()
            self.assertGreater(
                len(content), 0, f"{node_type}/prompts/sample.txt should not be empty"
            )


class TestRequestPayload(unittest.TestCase):
    def test_payload_structure(self):
        """Verify the request payload matches Ollama API format."""
        for node_type in NODE_TYPES:
            fmt = load_format(node_type)
            prompt_path = os.path.join(
                BASE_DIR, node_type, "prompts", "sample.txt"
            )
            with open(prompt_path, "r") as f:
                prompt = f.read().strip()

            payload = {
                "model": "llama3.1:8b",
                "prompt": prompt,
                "stream": False,
                "format": fmt,
            }

            # Verify payload has all required Ollama fields
            self.assertIn("model", payload)
            self.assertIn("prompt", payload)
            self.assertIn("stream", payload)
            self.assertIn("format", payload)

            # Verify format is a valid JSON Schema object
            self.assertEqual(payload["format"]["type"], "object")
            self.assertIn("properties", payload["format"])
            self.assertIn("required", payload["format"])

            # Verify it serializes to valid JSON
            serialized = json.dumps(payload)
            self.assertIsInstance(json.loads(serialized), dict)


if __name__ == "__main__":
    unittest.main()
