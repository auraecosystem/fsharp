model_list:
  - model_name: sonnet-4              # Deployment 1
    litellm_params:
      model: anthropic/claude-sonnet-5
      api_key: <our-real-key>
      
  - model_name: byok-sonnet-4         # Deployment 2  
    litellm_params:
      model: anthropic/claude-sonnet-5
      api_key: <customer-managed-key>
      api_base: https://proxy.litellm.ai/api.anthropic.com
      
  - model_name: sonnet-4              # Deployment 3
    litellm_params:
      model: vertex_ai/claude-sonnet-5
      vertex_project: my-project
