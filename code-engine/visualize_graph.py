import os
from app.workflow.graph import build_graph

def main():
    # Compile the graph
    app = build_graph()
    
    # Generate the Mermaid diagram as a PNG
    try:
        png_data = app.get_graph().draw_mermaid_png()
        output_path = "langgraph_visualization.png"
        
        with open(output_path, "wb") as f:
            f.write(png_data)
            
        print(f"Successfully saved graph visualization to: {os.path.abspath(output_path)}")
        
    except Exception as e:
        print(f"Failed to generate PNG (you might be missing dependencies like grandalf or graphviz): {e}")
        
        # Fallback: Just print the Mermaid syntax so the user can paste it into a viewer
        print("\nFallback: Here is the Mermaid syntax. You can paste this into https://mermaid.live/")
        print("-" * 40)
        print(app.get_graph().draw_mermaid())
        print("-" * 40)

if __name__ == "__main__":
    main()
