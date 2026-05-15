import sys
import xml.etree.ElementTree as ET

if len(sys.argv) != 3:
    print("Usage: python nunit_to_junit.py input.xml output.xml")
    sys.exit(1)

input_file = sys.argv[1]
output_file = sys.argv[2]

tree = ET.parse(input_file)
root = tree.getroot()

testsuite = ET.Element("testsuite")

testsuite.set("name", root.attrib.get("name", "UnityTests"))
testsuite.set("tests", root.attrib.get("total", "0"))
testsuite.set("failures", root.attrib.get("failed", "0"))
testsuite.set("errors", "0")
testsuite.set("skipped", root.attrib.get("skipped", "0"))
testsuite.set("time", root.attrib.get("duration", "0").replace(",", "."))

for testcase in root.iter("test-case"):

    classname = testcase.attrib.get("classname", "")
    name = testcase.attrib.get("name", "")
    duration = testcase.attrib.get("duration", "0").replace(",", ".")

    tc = ET.SubElement(testsuite, "testcase")
    tc.set("classname", classname)
    tc.set("name", name)
    tc.set("time", duration)

    result = testcase.attrib.get("result", "")

    if result == "Failed":

        failure = ET.SubElement(tc, "failure")

        message = ""
        stacktrace = ""

        failure_node = testcase.find("failure")

        if failure_node is not None:

            message_node = failure_node.find("message")
            stack_node = failure_node.find("stack-trace")

            if message_node is not None and message_node.text:
                message = message_node.text

            if stack_node is not None and stack_node.text:
                stacktrace = stack_node.text

        failure.text = f"{message}\n{stacktrace}"

testsuites = ET.Element("testsuites")
testsuites.append(testsuite)

ET.ElementTree(testsuites).write(output_file, encoding="utf-8", xml_declaration=True)

print(f"Converted {input_file} -> {output_file}")