# python3.10
import torch.onnx
import collections
import sample_factory.algo.learning.learner as Learner

checkpoint : dict = torch.load("VsStatic11RNN.pth")
print(checkpoint.keys()) # dict_keys(['train_step', 'env_steps', 'best_performance', 'model', 'optimizer', 'curr_lr'])
print(type(checkpoint["model"])) # model, optimizer - důležité?

modelA : collections.OrderedDict = checkpoint["model"]
print(modelA.keys())

learner = Learner.Learner() 

learner._load_state(learner.load_checkpoint(checkpoint, torch.device('cpu')))
print(learner)

#dummy_input = torch.randn(1, input_size, requires_grad=True)  

#print(model["optimizer"]["state"].keys()) #param_groups
#rint(checkpoint["curr_lr"])

#torch.onnx.export(modelA
#)