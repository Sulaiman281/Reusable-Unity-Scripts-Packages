import onnxruntime as ort
import numpy as np

# Path to your ONNX model
model_path = "cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4/model.onnx"

# Create an inference session
sess = ort.InferenceSession(model_path, providers=["CPUExecutionProvider"])
