const scene = document.querySelector(".workbench-scene");

if (scene && window.matchMedia("(pointer: fine)").matches) {
  window.addEventListener("mousemove", event => {
    const x = (event.clientX / window.innerWidth - 0.5) * 8;
    const y = (event.clientY / window.innerHeight - 0.5) * 5;
    scene.style.transform = `perspective(1600px) rotateX(${4 - y}deg) rotateY(${x}deg)`;
  });

  window.addEventListener("mouseleave", () => {
    scene.style.transform = "perspective(1600px) rotateX(4deg)";
  });
}
